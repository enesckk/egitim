using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Planning.Features;

public class CreateCoachTaskHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IStudentDirectory studentDirectory,
    ICoachStudentQuery coachQuery,
    IAcademicCatalog academicCatalog,
    INotificationService? notificationService = null)
{
    public async Task<CoachTaskDto> HandleAsync(Guid studentId, CreateCoachTaskCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            throw new ArgumentException("Task title cannot be empty.");
        }

        var student = await studentDirectory.GetStudentAsync(studentId, ct)
            ?? throw new NotFoundException("Student not found.");

        await ValidateCoachAccessAsync(student, ct);

        string? subjectName = null;
        if (command.SubjectId.HasValue)
        {
            var subjects = await academicCatalog.GetSubjectNamesAsync([command.SubjectId.Value], ct);
            if (subjects.TryGetValue(command.SubjectId.Value, out var sName))
            {
                subjectName = sName;
            }
        }

        string? topicName = null;
        if (command.TopicId.HasValue)
        {
            var topics = await academicCatalog.GetTopicNamesAsync([command.TopicId.Value], ct);
            if (topics.TryGetValue(command.TopicId.Value, out var tName))
            {
                topicName = tName;
            }
        }

        var task = new CoachTask
        {
            Id = Guid.NewGuid(),
            InstitutionId = student.InstitutionId,
            StudentId = student.Id,
            CoachUserId = currentUser.UserId!.Value,
            Title = command.Title.Trim(),
            Description = command.Description?.Trim(),
            DueDate = command.DueDate,
            DurationMinutes = command.DurationMinutes,
            SubjectId = command.SubjectId,
            SubjectName = subjectName,
            TopicId = command.TopicId,
            TopicName = topicName,
            LearningOutcomeId = command.LearningOutcomeId,
            LearningOutcomeName = null,
            Status = CoachTaskStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Set<CoachTask>().Add(task);
        await db.SaveChangesAsync(ct);

        if (notificationService != null && student.UserId.HasValue)
        {
            await notificationService.CreateNotificationAsync(
                institutionId: student.InstitutionId,
                userId: student.UserId.Value,
                type: NotificationType.CoachTaskAssigned,
                title: "Yeni Koç Görevi Atandı",
                message: $"'{task.Title}' başlıklı yeni bir koç görevi atandı.",
                relatedEntityType: "CoachTask",
                relatedEntityId: task.Id,
                ct: ct);
        }

        return MapToDto(task);
    }

    private async Task ValidateCoachAccessAsync(StudentReferenceDto student, CancellationToken ct)
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
            throw new ForbiddenException("Parents cannot create coach tasks.");
        }

        if (currentUser.IsInRole(Roles.Student))
        {
            throw new ForbiddenException("Students cannot create coach tasks.");
        }

        if (currentUser.IsInRole(Roles.Teacher) && !currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            throw new ForbiddenException("Teacher role cannot create coach tasks.");
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

    public static CoachTaskDto MapToDto(CoachTask t)
    {
        return new CoachTaskDto(
            t.Id,
            t.InstitutionId,
            t.StudentId,
            t.CoachUserId,
            t.Title,
            t.Description,
            t.DueDate,
            t.DurationMinutes,
            t.SubjectId,
            t.SubjectName,
            t.TopicId,
            t.TopicName,
            t.LearningOutcomeId,
            t.LearningOutcomeName,
            t.Status,
            IsCompleted: t.Status == CoachTaskStatus.Completed,
            t.CreatedAt,
            t.CompletedAt,
            t.UpdatedAt
        );
    }
}
