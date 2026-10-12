using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Planning.Entities;

namespace EgitimPlatform.Modules.Planning.Features;

public record AcceptAcademicStudyPlanCommand(
    Guid StudentId,
    Guid? ExamTypeId = null,
    int WindowSize = 3,
    int? Limit = 50,
    int RecommendationLimit = 10,
    int PlanDays = 7,
    int DailyStudyMinutes = 120,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null
);

public record UpdateTaskCompletionCommand(
    bool IsCompleted
);

public record StudyPlanDto(
    Guid Id,
    Guid InstitutionId,
    Guid StudentId,
    string Title,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    StudyPlanStatus Status,
    StudyPlanSource Source,
    string RecommendationPolicyVersion,
    string StudyPlanPolicyVersion,
    DateTimeOffset? ActivatedAt,
    DateTimeOffset? CompletedAt,
    int PlanDays,
    int DailyStudyMinutes,
    int SessionMinutes,
    int TotalAvailableMinutes,
    int TotalPlannedMinutes,
    int UnallocatedMinutes,
    decimal UtilizationRate,
    int ScheduledRecommendationCount,
    int UnscheduledRecommendationCount,
    int CompletedTaskCount,
    int TotalTaskCount,
    int RemainingTaskCount,
    int CompletedTaskMinutes,
    decimal TaskProgressRate,
    decimal MinuteProgressRate,
    DateTimeOffset CreatedAt
);

public record StudyTaskDto(
    Guid Id,
    Guid StudyPlanId,
    Guid StudentId,
    int DayIndex,
    DateTimeOffset? PlannedDate,
    int Sequence,
    int DurationMinutes,
    RecommendationNodeType NodeType,
    Guid NodeId,
    Guid? SubjectId,
    string? SubjectName,
    Guid? TopicId,
    string? TopicName,
    Guid? SubTopicId,
    string? SubTopicName,
    Guid? LearningOutcomeId,
    string? LearningOutcomeName,
    string TitleSnapshot,
    AcademicClassificationState ClassificationStateSnapshot,
    int PriorityScoreSnapshot,
    AcademicRecommendationType RecommendationType,
    bool IsCompleted,
    DateTimeOffset? CompletedAt
);

public record StudyPlanDetailDto(
    StudyPlanDto Plan,
    List<StudyTaskDto> Tasks
);
