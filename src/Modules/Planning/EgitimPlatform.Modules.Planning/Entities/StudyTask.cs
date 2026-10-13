using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

namespace EgitimPlatform.Modules.Planning.Entities;

public class StudyTask : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid StudyPlanId { get; set; }
    public Guid StudentId { get; set; }
    public int DayIndex { get; set; }
    public DateTimeOffset? PlannedDate { get; set; }
    public int Sequence { get; set; }
    public int DurationMinutes { get; set; }
    public RecommendationNodeType NodeType { get; set; }
    public Guid NodeId { get; set; }
    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }
    public Guid? SubTopicId { get; set; }
    public string? SubTopicName { get; set; }
    public Guid? LearningOutcomeId { get; set; }
    public string? LearningOutcomeName { get; set; }
    public string TitleSnapshot { get; set; } = string.Empty;
    public AcademicClassificationState ClassificationStateSnapshot { get; set; }
    public int PriorityScoreSnapshot { get; set; }
    public AcademicRecommendationType RecommendationType { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public StudyPlan? StudyPlan { get; set; }
}
