using EgitimPlatform.BuildingBlocks.Authorization;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Teachers.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Teachers.Features.ManageClassrooms;

public class ClassroomHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IStudentDirectory studentDirectory,
    IAuditService audit)
{
    public async Task<ClassroomDto> CreateAsync(CreateClassroomCommand command, CancellationToken ct = default)
    {
        await new CreateClassroomValidator().ValidateAndThrowAsync(command, ct);
        var institutionId = command.InstitutionId ?? await currentUser.GetInstitutionIdAsync() ?? Guid.Empty;
        await InstitutionManagement.DemandAsync(currentUser, institutionId);

        var exists = await db.Set<Classroom>()
            .AnyAsync(c => c.InstitutionId == institutionId && c.Name == command.Name.Trim() && !c.IsDeleted, ct);
        if (exists)
            throw new ConflictException($"A classroom with name '{command.Name.Trim()}' already exists in this institution.");

        var classroom = new Classroom
        {
            InstitutionId = institutionId,
            Name = command.Name.Trim(),
            Branch = string.IsNullOrWhiteSpace(command.Branch) ? null : command.Branch.Trim(),
            GradeLevel = command.GradeLevel,
            AcademicYear = string.IsNullOrWhiteSpace(command.AcademicYear) ? null : command.AcademicYear.Trim(),
            CreatedBy = currentUser.UserId
        };

        db.Set<Classroom>().Add(classroom);
        await audit.AddPendingLogAsync(
            currentUser.UserId!.Value,
            "Classroom.Created",
            "Classroom",
            classroom.Id.ToString(),
            classroom.InstitutionId);

        await db.SaveChangesAsync(ct);

        return new ClassroomDto(
            classroom.Id,
            classroom.InstitutionId,
            classroom.Name,
            classroom.Branch,
            classroom.GradeLevel,
            classroom.AcademicYear,
            0,
            0,
            []);
    }

    public async Task<IReadOnlyList<ClassroomDto>> ListAsync(ListClassroomsQuery query, CancellationToken ct = default)
    {
        await new ListClassroomsQueryValidator().ValidateAndThrowAsync(query, ct);

        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new ForbiddenException("Access denied.");

        var institutionId = await currentUser.GetInstitutionIdAsync();
        if (!currentUser.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Institution context required.");

        var classroomsQuery = db.Set<Classroom>().AsNoTracking().Where(c => !c.IsDeleted);

        if (!currentUser.IsSuperAdmin)
        {
            classroomsQuery = classroomsQuery.Where(c => c.InstitutionId == institutionId!.Value);
        }

        if (currentUser.IsInRole(Roles.Teacher) && !currentUser.IsSuperAdmin && !currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            var teacher = await db.Set<Teacher>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUser.UserId.Value && t.InstitutionId == institutionId!.Value && !t.IsDeleted, ct);

            if (teacher is null) return [];

            var assignedClassroomIds = await db.Set<ClassroomTeacher>()
                .AsNoTracking()
                .Where(ctLink => ctLink.TeacherId == teacher.Id && ctLink.InstitutionId == institutionId!.Value && !ctLink.IsDeleted)
                .Select(ctLink => ctLink.ClassroomId)
                .ToListAsync(ct);

            classroomsQuery = classroomsQuery.Where(c => assignedClassroomIds.Contains(c.Id));
        }
        else if (!currentUser.IsSuperAdmin && !currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            throw new ForbiddenException("Access denied.");
        }

        if (query.GradeLevel.HasValue)
            classroomsQuery = classroomsQuery.Where(c => c.GradeLevel == query.GradeLevel.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            classroomsQuery = classroomsQuery.Where(c =>
                c.Name.Contains(term) ||
                (c.Branch != null && c.Branch.Contains(term)));
        }

        var classrooms = await classroomsQuery
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        if (classrooms.Count == 0) return [];

        var classroomIds = classrooms.Select(c => c.Id).ToList();

        var teachers = await (from ctLink in db.Set<ClassroomTeacher>().AsNoTracking()
                              join t in db.Set<Teacher>().AsNoTracking() on ctLink.TeacherId equals t.Id
                              where classroomIds.Contains(ctLink.ClassroomId) && !ctLink.IsDeleted && !t.IsDeleted
                              select new
                              {
                                  ctLink.ClassroomId,
                                  TeacherId = t.Id,
                                  FullName = t.FirstName + " " + t.LastName,
                                  t.Title,
                                  ctLink.IsPrimary
                              }).ToListAsync(ct);

        var teachersByClassroom = teachers
            .GroupBy(x => x.ClassroomId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ClassroomTeacherDto>)g.Select(x => new ClassroomTeacherDto(
                    x.TeacherId,
                    x.FullName,
                    x.Title,
                    x.IsPrimary)).ToList());

        var studentCounts = await db.Set<ClassroomStudent>().AsNoTracking()
            .Where(cs => classroomIds.Contains(cs.ClassroomId) && !cs.IsDeleted)
            .GroupBy(cs => cs.ClassroomId)
            .Select(g => new { ClassroomId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassroomId, x => x.Count, ct);

        return classrooms.Select(c =>
        {
            var teacherList = teachersByClassroom.GetValueOrDefault(c.Id, []);
            var count = studentCounts.GetValueOrDefault(c.Id, 0);

            return new ClassroomDto(
                c.Id,
                c.InstitutionId,
                c.Name,
                c.Branch,
                c.GradeLevel,
                c.AcademicYear,
                count,
                teacherList.Count,
                teacherList);
        }).ToList();
    }

    public async Task<ClassroomDetailDto> GetAsync(Guid classroomId, CancellationToken ct = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new ForbiddenException("Access denied.");

        var institutionId = await currentUser.GetInstitutionIdAsync();
        if (!currentUser.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Institution context required.");

        var classrooms = db.Set<Classroom>().AsNoTracking().Where(c => c.Id == classroomId && !c.IsDeleted);
        if (!currentUser.IsSuperAdmin)
            classrooms = classrooms.Where(c => c.InstitutionId == institutionId!.Value);

        var classroom = await classrooms.FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Classroom", classroomId);

        if (currentUser.IsInRole(Roles.Teacher) && !currentUser.IsSuperAdmin && !currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            var teacher = await db.Set<Teacher>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == currentUser.UserId.Value && t.InstitutionId == classroom.InstitutionId && !t.IsDeleted, ct);

            if (teacher is null)
                throw new ForbiddenException("Access denied.");

            var hasAssignment = await db.Set<ClassroomTeacher>()
                .AnyAsync(ctLink => ctLink.ClassroomId == classroom.Id && ctLink.TeacherId == teacher.Id && ctLink.InstitutionId == classroom.InstitutionId && !ctLink.IsDeleted, ct);

            if (!hasAssignment)
                throw new ForbiddenException("Access denied.");
        }
        else if (!currentUser.IsSuperAdmin && !currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            throw new ForbiddenException("Access denied.");
        }

        var teachers = await (from ctLink in db.Set<ClassroomTeacher>().AsNoTracking()
                              join t in db.Set<Teacher>().AsNoTracking() on ctLink.TeacherId equals t.Id
                              where ctLink.ClassroomId == classroom.Id && !ctLink.IsDeleted && !t.IsDeleted
                              orderby ctLink.IsPrimary descending, t.LastName, t.FirstName
                              select new ClassroomTeacherDto(
                                  t.Id,
                                  t.FirstName + " " + t.LastName,
                                  t.Title,
                                  ctLink.IsPrimary))
                              .ToListAsync(ct);

        var studentIds = await db.Set<ClassroomStudent>().AsNoTracking()
            .Where(cs => cs.ClassroomId == classroom.Id && !cs.IsDeleted)
            .Select(cs => cs.StudentId)
            .ToListAsync(ct);

        var students = await studentDirectory.GetStudentsByIdsAsync(studentIds, classroom.InstitutionId, ct);

        var studentDtos = students.Select(s => new ClassroomStudentItemDto(
            s.Id,
            s.FirstName + " " + s.LastName,
            s.StudentNumber,
            s.GradeLevel)).ToList();

        return new ClassroomDetailDto(
            classroom.Id,
            classroom.InstitutionId,
            classroom.Name,
            classroom.Branch,
            classroom.GradeLevel,
            classroom.AcademicYear,
            teachers,
            studentDtos);
    }

    public async Task<ClassroomTeacherDto> AssignTeacherAsync(AssignClassroomTeacherCommand command, CancellationToken ct = default)
    {
        await new AssignClassroomTeacherValidator().ValidateAndThrowAsync(command, ct);

        var classroom = await db.Set<Classroom>()
            .FirstOrDefaultAsync(c => c.Id == command.ClassroomId && !c.IsDeleted, ct)
            ?? throw new NotFoundException("Classroom", command.ClassroomId);

        await InstitutionManagement.DemandAsync(currentUser, classroom.InstitutionId);

        var teacher = await db.Set<Teacher>()
            .FirstOrDefaultAsync(t => t.Id == command.TeacherId && t.InstitutionId == classroom.InstitutionId && !t.IsDeleted, ct)
            ?? throw new NotFoundException("Teacher", command.TeacherId);

        var existing = await db.Set<ClassroomTeacher>()
            .FirstOrDefaultAsync(ctLink => ctLink.ClassroomId == command.ClassroomId && ctLink.TeacherId == command.TeacherId && ctLink.InstitutionId == classroom.InstitutionId, ct);

        if (existing is not null)
        {
            if (!existing.IsDeleted)
                throw new ConflictException("Teacher is already assigned to this classroom.");

            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            existing.IsPrimary = command.IsPrimary;
            existing.UpdatedBy = currentUser.UserId;
        }
        else
        {
            existing = new ClassroomTeacher
            {
                InstitutionId = classroom.InstitutionId,
                ClassroomId = classroom.Id,
                TeacherId = teacher.Id,
                IsPrimary = command.IsPrimary,
                CreatedBy = currentUser.UserId
            };
            db.Set<ClassroomTeacher>().Add(existing);
        }

        await audit.AddPendingLogAsync(
            currentUser.UserId!.Value,
            "ClassroomTeacher.Assigned",
            "ClassroomTeacher",
            existing.Id.ToString(),
            classroom.InstitutionId);

        await db.SaveChangesAsync(ct);

        return new ClassroomTeacherDto(
            teacher.Id,
            teacher.FirstName + " " + teacher.LastName,
            teacher.Title,
            existing.IsPrimary);
    }

    public async Task RemoveTeacherAsync(Guid classroomId, Guid teacherId, CancellationToken ct = default)
    {
        var classroom = await db.Set<Classroom>()
            .FirstOrDefaultAsync(c => c.Id == classroomId && !c.IsDeleted, ct)
            ?? throw new NotFoundException("Classroom", classroomId);

        await InstitutionManagement.DemandAsync(currentUser, classroom.InstitutionId);

        var link = await db.Set<ClassroomTeacher>()
            .FirstOrDefaultAsync(ctLink => ctLink.ClassroomId == classroomId && ctLink.TeacherId == teacherId && ctLink.InstitutionId == classroom.InstitutionId && !ctLink.IsDeleted, ct)
            ?? throw new NotFoundException("ClassroomTeacher", teacherId);

        link.IsDeleted = true;
        link.DeletedAt = DateTimeOffset.UtcNow;
        link.DeletedBy = currentUser.UserId;

        await audit.AddPendingLogAsync(
            currentUser.UserId!.Value,
            "ClassroomTeacher.Removed",
            "ClassroomTeacher",
            link.Id.ToString(),
            classroom.InstitutionId);

        await db.SaveChangesAsync(ct);
    }

    public async Task<ClassroomStudentItemDto> AssignStudentAsync(AssignClassroomStudentCommand command, CancellationToken ct = default)
    {
        await new AssignClassroomStudentValidator().ValidateAndThrowAsync(command, ct);

        var classroom = await db.Set<Classroom>()
            .FirstOrDefaultAsync(c => c.Id == command.ClassroomId && !c.IsDeleted, ct)
            ?? throw new NotFoundException("Classroom", command.ClassroomId);

        await InstitutionManagement.DemandAsync(currentUser, classroom.InstitutionId);

        var student = await studentDirectory.GetStudentAsync(command.StudentId, ct);
        if (student is null || student.InstitutionId != classroom.InstitutionId)
            throw new NotFoundException("Student", command.StudentId);

        var existing = await db.Set<ClassroomStudent>()
            .FirstOrDefaultAsync(cs => cs.ClassroomId == command.ClassroomId && cs.StudentId == command.StudentId && cs.InstitutionId == classroom.InstitutionId, ct);

        if (existing is not null)
        {
            if (!existing.IsDeleted)
                throw new ConflictException("Student is already enrolled in this classroom.");

            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            existing.UpdatedBy = currentUser.UserId;
        }
        else
        {
            existing = new ClassroomStudent
            {
                InstitutionId = classroom.InstitutionId,
                ClassroomId = classroom.Id,
                StudentId = student.Id,
                CreatedBy = currentUser.UserId
            };
            db.Set<ClassroomStudent>().Add(existing);
        }

        await audit.AddPendingLogAsync(
            currentUser.UserId!.Value,
            "ClassroomStudent.Enrolled",
            "ClassroomStudent",
            existing.Id.ToString(),
            classroom.InstitutionId);

        await db.SaveChangesAsync(ct);

        var studentDetails = await studentDirectory.GetStudentsByIdsAsync([student.Id], classroom.InstitutionId, ct);
        var s = studentDetails.FirstOrDefault();

        return new ClassroomStudentItemDto(
            student.Id,
            s is not null ? $"{s.FirstName} {s.LastName}" : "Öğrenci",
            s?.StudentNumber,
            s?.GradeLevel);
    }

    public async Task RemoveStudentAsync(Guid classroomId, Guid studentId, CancellationToken ct = default)
    {
        var classroom = await db.Set<Classroom>()
            .FirstOrDefaultAsync(c => c.Id == classroomId && !c.IsDeleted, ct)
            ?? throw new NotFoundException("Classroom", classroomId);

        await InstitutionManagement.DemandAsync(currentUser, classroom.InstitutionId);

        var link = await db.Set<ClassroomStudent>()
            .FirstOrDefaultAsync(cs => cs.ClassroomId == classroomId && cs.StudentId == studentId && cs.InstitutionId == classroom.InstitutionId && !cs.IsDeleted, ct)
            ?? throw new NotFoundException("ClassroomStudent", studentId);

        link.IsDeleted = true;
        link.DeletedAt = DateTimeOffset.UtcNow;
        link.DeletedBy = currentUser.UserId;

        await audit.AddPendingLogAsync(
            currentUser.UserId!.Value,
            "ClassroomStudent.Removed",
            "ClassroomStudent",
            link.Id.ToString(),
            classroom.InstitutionId);

        await db.SaveChangesAsync(ct);
    }
}
