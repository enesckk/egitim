using EgitimPlatform.Modules.Planning.Entities;

namespace EgitimPlatform.Modules.Planning.Features;

public record CoachTaskDto(
    Guid Id,
    Guid InstitutionId,
    Guid StudentId,
    Guid CoachUserId,
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    int? DurationMinutes,
    Guid? SubjectId,
    string? SubjectName,
    Guid? TopicId,
    string? TopicName,
    Guid? LearningOutcomeId,
    string? LearningOutcomeName,
    CoachTaskStatus Status,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? UpdatedAt
);

public record CreateCoachTaskCommand(
    string Title,
    string? Description = null,
    DateTimeOffset? DueDate = null,
    int? DurationMinutes = null,
    Guid? SubjectId = null,
    Guid? TopicId = null,
    Guid? LearningOutcomeId = null
);

public record UpdateCoachTaskCommand(
    string Title,
    string? Description = null,
    DateTimeOffset? DueDate = null,
    int? DurationMinutes = null,
    Guid? SubjectId = null,
    Guid? TopicId = null,
    Guid? LearningOutcomeId = null
);

public record UpdateCoachTaskCompletionCommand(
    bool IsCompleted
);
