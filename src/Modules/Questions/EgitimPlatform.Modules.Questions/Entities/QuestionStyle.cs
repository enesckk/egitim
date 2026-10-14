using EgitimPlatform.BuildingBlocks.Entities;

namespace EgitimPlatform.Modules.Questions.Entities;

/// <summary>
/// Controlled taxonomy for question styles/types (e.g. NEW_GENERATION, CONCEPTUAL, etc.).
/// Free-text tags are prohibited; all styles are controlled and identified by a unique code.
/// </summary>
public class QuestionStyle : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    public List<QuestionStyleAssignment> QuestionAssignments { get; set; } = [];
}
