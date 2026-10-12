using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Planning.Features;

public class UpdateCoachTaskCompletionHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IStudentDirectory studentDirectory)
{
    public async Task<CoachTaskDto> HandleAsync(Guid studentId, Guid taskId, UpdateCoachTaskCompletionCommand command, CancellationToken ct = default)
    {
        var student = await studentDirectory.GetStudentAsync(studentId, ct)
            ?? throw new NotFoundException("Student not found.");

        await ValidateStudentAccessAsync(student);

        var task = await db.Set<CoachTask>()
            .FirstOrDefaultAsync(t => t.Id == taskId && t.StudentId == studentId && t.InstitutionId == student.InstitutionId && !t.IsDeleted, ct)
            ?? throw new NotFoundException("Coach task not found.");

        if (task.Status == CoachTaskStatus.Cancelled)
        {
            throw new ConflictException("Cancelled tasks cannot be completed or modified.");
        }

        if (command.IsCompleted)
        {
            task.Status = CoachTaskStatus.Completed;
            task.CompletedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            task.Status = CoachTaskStatus.Active;
            task.CompletedAt = null;
        }

        task.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        return CreateCoachTaskHandler.MapToDto(task);
    }

    private async Task ValidateStudentAccessAsync(StudentReferenceDto student)
    {
        if (currentUser.IsInRole(Roles.Parent))
        {
            throw new ForbiddenException("Parent cannot update student task completion.");
        }

        if (currentUser.IsInRole(Roles.Coach))
        {
            throw new ForbiddenException("Coach cannot toggle task completion on behalf of student.");
        }

        if (currentUser.IsInRole(Roles.Teacher) && !currentUser.IsSuperAdmin && !currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            throw new ForbiddenException("Teacher role cannot update student task completion.");
        }

        if (currentUser.IsSuperAdmin) return;

        var currentInstId = await currentUser.GetInstitutionIdAsync();
        if (!currentInstId.HasValue || currentInstId.Value != student.InstitutionId)
        {
            throw new ForbiddenException("Access denied across institution boundary.");
        }

        if (currentUser.IsInRole(Roles.Student) && currentUser.UserId != student.UserId)
        {
            throw new ForbiddenException("Student can only update their own task completion.");
        }
    }
}
