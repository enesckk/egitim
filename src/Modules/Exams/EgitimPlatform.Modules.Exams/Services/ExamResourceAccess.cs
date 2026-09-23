using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Services;

public interface IExamResourceAccess
{
    Task<StudentReferenceDto> ValidateStudentReadAccessAsync(Guid studentId, CancellationToken ct = default);
    Task<StudentReferenceDto> ValidateStudentWriteAccessAsync(Guid studentId, CancellationToken ct = default);
    Task<Exam> ValidateExamAccessAsync(Guid examId, CancellationToken ct = default);
    Task<StudentExamAttempt> ValidateAttemptReadAccessAsync(Guid attemptId, CancellationToken ct = default);
    Task<StudentExamAttempt> ValidateAttemptWriteAccessAsync(Guid attemptId, CancellationToken ct = default);
}

public class ExamResourceAccess(
    IApplicationDbContext db,
    ICurrentUser user,
    IStudentDirectory studentDirectory,
    ICoachStudentQuery coachQuery) : IExamResourceAccess
{
    public async Task<StudentReferenceDto> ValidateStudentReadAccessAsync(Guid studentId, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || user.UserId is null)
            throw new ForbiddenException("User is not authenticated.");

        var student = await studentDirectory.GetStudentAsync(studentId, ct)
            ?? throw new NotFoundException("Student", studentId);

        if (!user.IsSuperAdmin)
        {
            var institutionId = await user.GetInstitutionIdAsync();
            if (!institutionId.HasValue || student.InstitutionId != institutionId.Value)
                throw new NotFoundException("Student", studentId); // foreign tenant = 404
        }

        // Teacher role is FAIL CLOSED for student exam results
        if (user.IsInRole(Roles.Teacher) && !user.IsSuperAdmin && !user.IsInRole(Roles.InstitutionAdmin))
            throw new ForbiddenException("Teacher role cannot access student exam results.");

        if (user.IsSuperAdmin) return student;

        var instId = (await user.GetInstitutionIdAsync())!.Value;

        if (user.IsInRole(Roles.InstitutionAdmin))
            return student;

        if (user.IsInRole(Roles.Student))
        {
            if (student.UserId != user.UserId.Value)
                throw new ForbiddenException("Access denied.");
            return student;
        }

        if (user.IsInRole(Roles.Coach))
        {
            var hasAssignment = await coachQuery.HasActiveAssignmentAsync(user.UserId.Value, instId, student.Id, ct);
            if (!hasAssignment)
                throw new ForbiddenException("Access denied.");
            return student;
        }

        if (user.IsInRole(Roles.Parent))
        {
            var isParentRelated = await studentDirectory.IsParentRelatedToStudentAsync(user.UserId.Value, studentId, instId, ct);
            if (!isParentRelated)
                throw new ForbiddenException("Access denied.");
            return student;
        }

        throw new ForbiddenException("Access denied.");
    }

    public async Task<StudentReferenceDto> ValidateStudentWriteAccessAsync(Guid studentId, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || user.UserId is null)
            throw new ForbiddenException("User is not authenticated.");

        var student = await studentDirectory.GetStudentAsync(studentId, ct)
            ?? throw new NotFoundException("Student", studentId);

        if (!user.IsSuperAdmin)
        {
            var institutionId = await user.GetInstitutionIdAsync();
            if (!institutionId.HasValue || student.InstitutionId != institutionId.Value)
                throw new NotFoundException("Student", studentId); // foreign tenant = 404
        }

        // Parents and Teachers cannot record/update exam results
        if (user.IsInRole(Roles.Parent) || user.IsInRole(Roles.Teacher))
            throw new ForbiddenException("Write access denied for role.");

        if (user.IsSuperAdmin) return student;

        var instId = (await user.GetInstitutionIdAsync())!.Value;

        if (user.IsInRole(Roles.InstitutionAdmin))
            return student;

        if (user.IsInRole(Roles.Coach))
        {
            var hasAssignment = await coachQuery.HasActiveAssignmentAsync(user.UserId.Value, instId, student.Id, ct);
            if (!hasAssignment)
                throw new ForbiddenException("Access denied.");
            return student;
        }

        if (user.IsInRole(Roles.Student))
        {
            if (student.UserId != user.UserId.Value)
                throw new ForbiddenException("Access denied.");
            return student;
        }

        throw new ForbiddenException("Access denied.");
    }

    public async Task<Exam> ValidateExamAccessAsync(Guid examId, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated)
            throw new ForbiddenException("User is not authenticated.");

        var query = db.Set<Exam>().AsNoTracking().Where(e => e.Id == examId);
        if (!user.IsSuperAdmin)
        {
            var institutionId = await user.GetInstitutionIdAsync();
            if (!institutionId.HasValue)
                throw new ForbiddenException("Access denied.");
            query = query.Where(e => e.InstitutionId == institutionId.Value);
        }

        return await query.SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Exam", examId);
    }

    public async Task<StudentExamAttempt> ValidateAttemptReadAccessAsync(Guid attemptId, CancellationToken ct = default)
    {
        var attemptQuery = db.Set<StudentExamAttempt>().AsNoTracking().Where(a => a.Id == attemptId);
        if (!user.IsSuperAdmin)
        {
            var institutionId = await user.GetInstitutionIdAsync();
            if (!institutionId.HasValue)
                throw new ForbiddenException("Access denied.");
            attemptQuery = attemptQuery.Where(a => a.InstitutionId == institutionId.Value);
        }

        var attempt = await attemptQuery.SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("StudentExamAttempt", attemptId);

        await ValidateStudentReadAccessAsync(attempt.StudentId, ct);
        return attempt;
    }

    public async Task<StudentExamAttempt> ValidateAttemptWriteAccessAsync(Guid attemptId, CancellationToken ct = default)
    {
        var attemptQuery = db.Set<StudentExamAttempt>().Where(a => a.Id == attemptId);
        if (!user.IsSuperAdmin)
        {
            var institutionId = await user.GetInstitutionIdAsync();
            if (!institutionId.HasValue)
                throw new ForbiddenException("Access denied.");
            attemptQuery = attemptQuery.Where(a => a.InstitutionId == institutionId.Value);
        }

        var attempt = await attemptQuery.SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("StudentExamAttempt", attemptId);

        await ValidateStudentWriteAccessAsync(attempt.StudentId, ct);
        return attempt;
    }
}
