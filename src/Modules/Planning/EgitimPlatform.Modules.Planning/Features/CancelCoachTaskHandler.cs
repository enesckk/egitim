using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Planning.Features;

public class CancelCoachTaskHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IStudentDirectory studentDirectory,
    ICoachStudentQuery coachQuery)
{
    public async Task<CoachTaskDto> HandleAsync(Guid studentId, Guid taskId, CancellationToken ct = default)
    {
        var student = await studentDirectory.GetStudentAsync(studentId, ct)
            ?? throw new NotFoundException("Student not found.");

        await ValidateCoachWriteAccessAsync(student, ct);

        var task = await db.Set<CoachTask>()
            .FirstOrDefaultAsync(t => t.Id == taskId && t.StudentId == studentId && t.InstitutionId == student.InstitutionId && !t.IsDeleted, ct)
            ?? throw new NotFoundException("Coach task not found.");

        if (task.Status != CoachTaskStatus.Active)
        {
            throw new ConflictException("Only active tasks can be cancelled.");
        }

        task.Status = CoachTaskStatus.Cancelled;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        return CreateCoachTaskHandler.MapToDto(task);
    }

    private async Task ValidateCoachWriteAccessAsync(StudentReferenceDto student, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new ForbiddenException("Authenticated context is missing user identity.");
        }

        if (currentUser.IsSuperAdmin) return;

        var currentInstId = await currentUser.GetInstitutionIdAsync();
        if (!currentInstId.HasValue || currentInstId.Value != student.InstitutionId)
        {
            throw new ForbiddenException("Access denied across institution boundary.");
        }

        if (currentUser.IsInRole(Roles.Parent))
        {
            throw new ForbiddenException("Parents cannot cancel coach tasks.");
        }

        if (currentUser.IsInRole(Roles.Student))
        {
            throw new ForbiddenException("Students cannot cancel coach tasks.");
        }

        if (currentUser.IsInRole(Roles.Teacher) && !currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            throw new ForbiddenException("Teacher role cannot cancel coach tasks.");
        }

        if (currentUser.IsInRole(Roles.Coach))
        {
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
    }
}
