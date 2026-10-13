using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Planning.Entities;

public class CoachTask : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid StudentId { get; set; }
    public Guid CoachUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public int? DurationMinutes { get; set; }
    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }
    public Guid? LearningOutcomeId { get; set; }
    public string? LearningOutcomeName { get; set; }
    public CoachTaskStatus Status { get; set; } = CoachTaskStatus.Active;
    public DateTimeOffset? CompletedAt { get; set; }
}
