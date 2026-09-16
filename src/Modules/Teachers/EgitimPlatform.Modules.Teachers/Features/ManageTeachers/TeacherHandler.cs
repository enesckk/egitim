using EgitimPlatform.BuildingBlocks.Authorization;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Teachers.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
namespace EgitimPlatform.Modules.Teachers.Features.ManageTeachers;
public class TeacherHandler(IApplicationDbContext db, ICurrentUser user, IUserDirectory users, IAcademicCatalog academic, IAuditService audit)
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
        if (!await users.IsActiveInInstitutionAsync(c.UserId, c.InstitutionId, Roles.Teacher, ct))
            throw new ValidationException("Teacher account must be active in the same institution with the Teacher role.");
        var teacher = new Teacher { InstitutionId = c.InstitutionId, UserId = c.UserId, FirstName = c.FirstName, LastName = c.LastName, Title = c.Title, CreatedBy = user.UserId };
        db.Set<Teacher>().Add(teacher);
        await audit.AddPendingLogAsync(user.UserId!.Value, "Teacher.Created", "Teacher", teacher.Id.ToString(), teacher.InstitutionId);
        await db.SaveChangesAsync(ct);
        return new(teacher.Id, teacher.FirstName, teacher.LastName);
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
