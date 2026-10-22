using EgitimPlatform.BuildingBlocks.Authorization;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Teachers.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
namespace EgitimPlatform.Modules.Teachers.Features.ManageTeachers;
public class TeacherHandler(IApplicationDbContext db, ICurrentUser user, IUserDirectory users, IAcademicCatalog academic, IAuditService audit, IUserProvisioner? userProvisioner = null)
{
    public async Task<TeacherDetailDto> GetAsync(Guid teacherId, CancellationToken ct)
    {
        var institutionId = await user.GetInstitutionIdAsync();
        if (!user.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Access denied.");

        var teachers = db.Set<Teacher>().AsNoTracking().Where(x => x.Id == teacherId);
        if (!user.IsSuperAdmin)
            teachers = teachers.Where(x => x.InstitutionId == institutionId!.Value);

        var teacher = await teachers
            .Select(x => new TeacherDetailDto(x.Id, x.UserId, x.FirstName, x.LastName, x.Title))
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Teacher", teacherId);
        return teacher;
    }
    public async Task<TeacherMeDto> GetMeAsync(CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || user.UserId is null)
            throw new ForbiddenException("Access denied.");

        var institutionId = await user.GetInstitutionIdAsync();
        if (!institutionId.HasValue)
            throw new ForbiddenException("Institution context required.");

        var teacher = await db.Set<Teacher>().AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == user.UserId.Value && t.InstitutionId == institutionId.Value && !t.IsDeleted, ct)
            ?? throw new NotFoundException("Teacher profile not found for user.");

        var subjects = await db.Set<TeacherSubject>().AsNoTracking()
            .Where(ts => ts.TeacherId == teacher.Id && ts.InstitutionId == teacher.InstitutionId && !ts.IsDeleted)
            .ToListAsync(ct);

        var subjectIds = subjects.Select(s => s.SubjectId).Distinct().ToList();
        var subjectNames = await academic.GetSubjectNamesAsync(subjectIds, ct);

        var subjectDtos = subjects.Select(s => new TeacherSubjectDetailDto(
            s.Id,
            s.SubjectId,
            subjectNames.GetValueOrDefault(s.SubjectId, "Unknown Subject"))).ToList();

        var classroomIds = await db.Set<ClassroomTeacher>().AsNoTracking()
            .Where(ctLink => ctLink.TeacherId == teacher.Id && ctLink.InstitutionId == teacher.InstitutionId && !ctLink.IsDeleted)
            .Select(ctLink => ctLink.ClassroomId)
            .Distinct()
            .ToListAsync(ct);

        var studentCount = await db.Set<ClassroomStudent>().AsNoTracking()
            .Where(cs => classroomIds.Contains(cs.ClassroomId) && cs.InstitutionId == teacher.InstitutionId && !cs.IsDeleted)
            .Select(cs => cs.StudentId)
            .Distinct()
            .CountAsync(ct);

        return new TeacherMeDto(
            teacher.Id,
            teacher.UserId,
            teacher.FirstName,
            teacher.LastName,
            $"{teacher.FirstName} {teacher.LastName}",
            teacher.Title,
            teacher.InstitutionId,
            subjectDtos,
            classroomIds.Count,
            studentCount);
    }
    public async Task<IReadOnlyList<TeacherListDto>> ListAsync(ListTeachersQuery q, CancellationToken ct)
    {
        await new ListTeachersQueryValidator().ValidateAndThrowAsync(q, ct);
        var institutionId = await user.GetInstitutionIdAsync();
        if (!user.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Access denied.");

        var teachers = db.Set<Teacher>().AsNoTracking();
        if (!user.IsSuperAdmin)
            teachers = teachers.Where(x => x.InstitutionId == institutionId!.Value);

        return await teachers
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(x => new TeacherListDto(x.Id, x.FirstName, x.LastName, x.Title))
            .ToListAsync(ct);
    }
    public async Task<IReadOnlyList<TeacherSubjectDto>> GetSubjectsAsync(Guid teacherId, CancellationToken ct)
    {
        if (teacherId == Guid.Empty) throw new ValidationException("Teacher identity is required.");
        var institutionId = await user.GetInstitutionIdAsync();
        if (!user.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Access denied.");

        var teachers = db.Set<Teacher>().AsNoTracking().Where(x => x.Id == teacherId);
        if (!user.IsSuperAdmin)
            teachers = teachers.Where(x => x.InstitutionId == institutionId!.Value);

        var teacher = await teachers.SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Teacher", teacherId);
        var isOwnTeacher = user.IsAuthenticated && user.IsInRole(Roles.Teacher) && user.UserId == teacher.UserId && institutionId == teacher.InstitutionId;
        if (!isOwnTeacher) await InstitutionManagement.DemandAsync(user, teacher.InstitutionId);
        return await db.Set<TeacherSubject>().AsNoTracking().Where(x => x.TeacherId == teacher.Id && x.InstitutionId == teacher.InstitutionId)
            .OrderBy(x => x.SubjectId).Select(x => new TeacherSubjectDto(x.Id, x.TeacherId, x.SubjectId)).ToListAsync(ct);
    }
    public async Task<TeacherDto> CreateAsync(CreateTeacherCommand c, CancellationToken ct)
    {
        await new CreateTeacherValidator().ValidateAndThrowAsync(c, ct);
        await InstitutionManagement.DemandAsync(user, c.InstitutionId);

        Guid targetUserId;
        Guid? provisionedUserId = null;

        await using var transaction = await db.BeginTransactionAsync(ct);
        try
        {
            if (c.UserId.HasValue)
            {
                if (!await users.IsActiveInInstitutionAsync(c.UserId.Value, c.InstitutionId, Roles.Teacher, ct))
                    throw new ValidationException("Teacher account must be active in the same institution with the Teacher role.");
                targetUserId = c.UserId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(c.Email) && !string.IsNullOrWhiteSpace(c.Password))
            {
                if (userProvisioner is null)
                    throw new InvalidOperationException("User provisioning service is unavailable.");

                provisionedUserId = await userProvisioner.ProvisionUserAsync(
                    c.Email,
                    c.Password,
                    c.FirstName,
                    c.LastName,
                    Roles.Teacher,
                    c.InstitutionId,
                    ct);

                targetUserId = provisionedUserId.Value;
            }
            else
            {
                throw new ValidationException("Either UserId or Email and Password must be provided.");
            }

            var teacher = new Teacher { InstitutionId = c.InstitutionId, UserId = targetUserId, FirstName = c.FirstName, LastName = c.LastName, Title = c.Title, CreatedBy = user.UserId };
            db.Set<Teacher>().Add(teacher);
            await audit.AddPendingLogAsync(user.UserId!.Value, "Teacher.Created", "Teacher", teacher.Id.ToString(), teacher.InstitutionId);

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return new(teacher.Id, teacher.FirstName, teacher.LastName);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            if (provisionedUserId.HasValue && userProvisioner is not null)
            {
                try
                {
                    await userProvisioner.DeleteProvisionedUserAsync(provisionedUserId.Value, ct);
                }
                catch
                {
                    // Secondary cleanup attempt; DB transaction rollback already guaranteed database consistency.
                }
            }
            throw;
        }
    }
    public async Task<TeacherSubjectDto> SetSubjectAsync(TeacherSubjectCommand c, bool remove, CancellationToken ct)
    {
        await new TeacherSubjectValidator().ValidateAndThrowAsync(c, ct);
        var institutionId = await user.GetInstitutionIdAsync();
        if (!user.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Access denied.");

        var teachers = db.Set<Teacher>().Where(x => x.Id == c.TeacherId);
        if (!user.IsSuperAdmin)
            teachers = teachers.Where(x => x.InstitutionId == institutionId!.Value);

        var teacher = await teachers.SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Teacher", c.TeacherId);
        await InstitutionManagement.DemandAsync(user, teacher.InstitutionId);
        if (!remove && !await academic.SubjectExistsAsync(c.SubjectId, ct)) throw new ValidationException("Unknown or inactive subject.");
        var link = await db.Set<TeacherSubject>().SingleOrDefaultAsync(x => x.TeacherId == c.TeacherId && x.SubjectId == c.SubjectId && x.InstitutionId == teacher.InstitutionId, ct);
        if (remove)
        {
            if (link is null) throw new NotFoundException("TeacherSubject", c.SubjectId);
            link.IsDeleted = true; link.DeletedAt = DateTimeOffset.UtcNow; link.DeletedBy = user.UserId;
        }
        else
        {
            if (link is not null) throw new ConflictException("Teacher subject already exists.");
            link = new TeacherSubject { TeacherId = teacher.Id, InstitutionId = teacher.InstitutionId, SubjectId = c.SubjectId, CreatedBy = user.UserId };
            db.Set<TeacherSubject>().Add(link);
        }
        await audit.AddPendingLogAsync(user.UserId!.Value, remove ? "TeacherSubject.Removed" : "TeacherSubject.Added", "TeacherSubject", link.Id.ToString(), link.InstitutionId);
        await db.SaveChangesAsync(ct);
        return new(link.Id, link.TeacherId, link.SubjectId);
    }
}
