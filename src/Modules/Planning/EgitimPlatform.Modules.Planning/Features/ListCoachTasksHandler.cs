using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Planning.Features;

public class ListCoachTasksHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IStudentDirectory studentDirectory,
    ICoachStudentQuery coachQuery)
{
    public async Task<List<CoachTaskDto>> HandleAsync(Guid studentId, CoachTaskStatus? status = null, CancellationToken ct = default)
    {
        var student = await studentDirectory.GetStudentAsync(studentId, ct)
            ?? throw new NotFoundException("Student not found.");

        await ValidateReadAccessAsync(student, ct);

        var query = db.Set<CoachTask>()
            .AsNoTracking()
            .Where(t => t.StudentId == studentId && t.InstitutionId == student.InstitutionId && !t.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return tasks.Select(CreateCoachTaskHandler.MapToDto).ToList();
    }

    private async Task ValidateReadAccessAsync(StudentReferenceDto student, CancellationToken ct)
    {
        if (currentUser.IsSuperAdmin) return;

        var currentInstId = await currentUser.GetInstitutionIdAsync();
        if (!currentInstId.HasValue || currentInstId.Value != student.InstitutionId)
        {
            throw new ForbiddenException("Access denied across institution boundary.");
        }

        if (currentUser.IsInRole(Roles.Teacher) && !currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            throw new ForbiddenException("Teacher role cannot access coach tasks.");
        }

        if (currentUser.IsInRole(Roles.Student) && currentUser.UserId != student.UserId)
        {
            throw new ForbiddenException("Student can only view their own coach tasks.");
        }

        if (currentUser.IsInRole(Roles.Coach))
        {
            if (!currentUser.UserId.HasValue)
            {
                throw new ForbiddenException("Authenticated context is missing user identity.");
            }

            var isAssigned = await coachQuery.HasActiveAssignmentAsync(
                currentUser.UserId.Value,
                currentInstId.Value,
                student.Id,
                ct);

            if (!isAssigned)
            {
                throw new ForbiddenException("Coach is not actively assigned to this student.");
            }
        }

        if (currentUser.IsInRole(Roles.Parent))
        {
            if (!currentUser.UserId.HasValue)
            {
                throw new ForbiddenException("Authenticated parent context is missing user identity.");
            }

            var isRelated = await studentDirectory.IsParentRelatedToStudentAsync(
                currentUser.UserId.Value,
                student.Id,
                currentInstId.Value,
                ct);

            if (!isRelated)
            {
                throw new ForbiddenException("Parent is not linked to this student.");
            }
        }
    }
}
