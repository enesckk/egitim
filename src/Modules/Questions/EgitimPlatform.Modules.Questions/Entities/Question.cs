using EgitimPlatform.BuildingBlocks.Entities;

namespace EgitimPlatform.Modules.Questions.Entities;

/// <summary>
/// Sprint 4A — Question entity representing a Question Bank item.
/// InstitutionId == null -> Global shared question pool (ÖSYM, platform pool).
/// InstitutionId == Guid -> Tenant-specific custom question.
/// </summary>
public class Question : SoftDeletableEntity
{
    public byte[] RowVersion { get; set; } = [];

    /// <summary>
    /// Nullable: null means Global Pool, non-null means Institution Private Pool.
    /// </summary>
    public Guid? InstitutionId { get; set; }

    public Guid SubjectId { get; set; }
    public Guid TopicId { get; set; }
    public Guid? SubTopicId { get; set; }
    public Guid? LearningOutcomeId { get; set; }

    public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;
    public QuestionSourceType SourceType { get; set; } = QuestionSourceType.InstitutionCustom;
    public int? SourceYear { get; set; }

    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public QuestionStatus Status { get; set; } = QuestionStatus.Draft;

    public List<QuestionOption> Options { get; set; } = [];
}
