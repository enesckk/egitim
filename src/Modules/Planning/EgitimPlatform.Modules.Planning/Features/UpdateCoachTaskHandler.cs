using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Planning.Features;

public class UpdateCoachTaskHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IStudentDirectory studentDirectory,
    ICoachStudentQuery coachQuery,
    IAcademicCatalog academicCatalog)
{
    public async Task<CoachTaskDto> HandleAsync(Guid studentId, Guid taskId, UpdateCoachTaskCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            throw new ArgumentException("Task title cannot be empty.");
        }

        var student = await studentDirectory.GetStudentAsync(studentId, ct)
            ?? throw new NotFoundException("Student not found.");

        await ValidateCoachWriteAccessAsync(student, ct);

        var task = await db.Set<CoachTask>()
            .FirstOrDefaultAsync(t => t.Id == taskId && t.StudentId == studentId && t.InstitutionId == student.InstitutionId && !t.IsDeleted, ct)
            ?? throw new NotFoundException("Coach task not found.");

        if (task.Status != CoachTaskStatus.Active)
        {
            throw new ConflictException("Completed or cancelled coach tasks are read-only and cannot be updated.");
        }

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

        task.Title = command.Title.Trim();
        task.Description = command.Description?.Trim();
        task.DueDate = command.DueDate;
        task.DurationMinutes = command.DurationMinutes;
        task.SubjectId = command.SubjectId;
        task.SubjectName = subjectName;
        task.TopicId = command.TopicId;
        task.TopicName = topicName;
        task.LearningOutcomeId = command.LearningOutcomeId;
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
            throw new ForbiddenException("Parents cannot edit coach tasks.");
        }

        if (currentUser.IsInRole(Roles.Student))
        {
            throw new ForbiddenException("Students cannot edit coach task contents.");
        }

        if (currentUser.IsInRole(Roles.Teacher) && !currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            throw new ForbiddenException("Teacher role cannot edit coach tasks.");
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
