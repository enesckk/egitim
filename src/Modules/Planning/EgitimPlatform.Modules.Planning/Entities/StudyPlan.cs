using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Planning.Entities;

public class StudyPlan : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid StudentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public StudyPlanStatus Status { get; set; } = StudyPlanStatus.Active;
    public StudyPlanSource Source { get; set; } = StudyPlanSource.AcademicIntelligence;
    public string RecommendationPolicyVersion { get; set; } = string.Empty;
    public string StudyPlanPolicyVersion { get; set; } = string.Empty;
    public DateTimeOffset? ActivatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int PlanDays { get; set; }
    public int DailyStudyMinutes { get; set; }
    public int SessionMinutes { get; set; }
    public int TotalAvailableMinutes { get; set; }
    public int TotalPlannedMinutes { get; set; }
    public int UnallocatedMinutes { get; set; }
    public decimal UtilizationRate { get; set; }
    public int ScheduledRecommendationCount { get; set; }
    public int UnscheduledRecommendationCount { get; set; }

    public ICollection<StudyTask> Tasks { get; set; } = new List<StudyTask>();
}
