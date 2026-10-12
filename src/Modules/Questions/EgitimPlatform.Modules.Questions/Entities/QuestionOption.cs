using EgitimPlatform.BuildingBlocks.Entities;

namespace EgitimPlatform.Modules.Questions.Entities;

/// <summary>
/// Sprint 4A — Option for a Question (e.g. A, B, C, D, E).
/// </summary>
public class QuestionOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    public string OptionLabel { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
