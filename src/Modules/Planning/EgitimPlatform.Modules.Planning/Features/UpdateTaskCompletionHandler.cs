using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Planning.Features;

public class UpdateTaskCompletionHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IStudentDirectory studentDirectory,
    INotificationService? notificationService = null)
{
    public async Task<StudyTaskDto> HandleAsync(Guid studentId, Guid planId, Guid taskId, UpdateTaskCompletionCommand command, CancellationToken ct = default)
    {
        var student = await studentDirectory.GetStudentAsync(studentId, ct)
            ?? throw new NotFoundException("Student not found.");

        await ValidateAccessAsync(student);

        var plan = await db.Set<StudyPlan>()
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == planId && p.StudentId == studentId && p.InstitutionId == student.InstitutionId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Study plan not found.");

        var task = plan.Tasks.FirstOrDefault(t => t.Id == taskId && !t.IsDeleted)
            ?? throw new NotFoundException("Study task not found.");

        if (plan.Status != StudyPlanStatus.Active)
        {
            throw new ConflictException("Cannot modify tasks of a study plan that is not active.");
        }

        var now = DateTimeOffset.UtcNow;

        if (command.IsCompleted)
        {
            task.IsCompleted = true;
            task.CompletedAt = now;
        }
        else
        {
            task.IsCompleted = false;
            task.CompletedAt = null;
        }

        // Automatic Plan Completion Evaluation
        var activeTasks = plan.Tasks.Where(t => !t.IsDeleted).ToList();
        var allCompleted = activeTasks.Count > 0 && activeTasks.All(t => t.IsCompleted);

        if (allCompleted)
        {
            plan.Status = StudyPlanStatus.Completed;
            plan.CompletedAt = now;
        }

        await db.SaveChangesAsync(ct);

        if (allCompleted && notificationService != null && student.UserId.HasValue)
        {
            await notificationService.CreateNotificationAsync(
                institutionId: student.InstitutionId,
                userId: student.UserId.Value,
                type: NotificationType.StudyPlanCompleted,
                title: "Çalışma Planı Tamamlandı",
                message: $"Tebrikler! '{plan.Title}' başlıklı çalışma planındaki tüm görevleri başarıyla tamamladınız.",
                relatedEntityType: "StudyPlan",
                relatedEntityId: plan.Id,
                ct: ct);
        }

        return new StudyTaskDto(
            task.Id,
            task.StudyPlanId,
            task.StudentId,
            task.DayIndex,
            task.PlannedDate,
            task.Sequence,
            task.DurationMinutes,
            task.NodeType,
            task.NodeId,
            task.SubjectId,
            task.SubjectName,
            task.TopicId,
            task.TopicName,
            task.SubTopicId,
            task.SubTopicName,
            task.LearningOutcomeId,
            task.LearningOutcomeName,
            task.TitleSnapshot,
            task.ClassificationStateSnapshot,
            task.PriorityScoreSnapshot,
            task.RecommendationType,
            task.IsCompleted,
            task.CompletedAt
        );
    }

    private async Task ValidateAccessAsync(StudentReferenceDto student)
    {
        if (currentUser.IsInRole(Roles.Parent))
        {
            throw new ForbiddenException("Parent cannot modify student study plan tasks.");
        }

        if (currentUser.IsInRole(Roles.Coach))
        {
            throw new ForbiddenException("Coach cannot modify student study plans.");
        }

        if (currentUser.IsInRole(Roles.Teacher) && !currentUser.IsSuperAdmin && !currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            throw new ForbiddenException("Teacher role cannot modify student study plans.");
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
