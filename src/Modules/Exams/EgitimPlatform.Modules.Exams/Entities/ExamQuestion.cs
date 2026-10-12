using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Exams.Entities;

/// <summary>
/// Sprint 4A — Link between ExamSection and a Question.
/// Holds a snapshot of CorrectOption at exam creation/publishing time.
/// </summary>
public class ExamQuestion : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid ExamId { get; set; }
    public Guid ExamSectionId { get; set; }
    public Guid QuestionId { get; set; }

    public int QuestionNumber { get; set; }
    public int Order { get; set; }

    /// <summary>
    /// Answer key snapshot (e.g. "A", "B", "C", "D", "E") at exam publishing time.
    /// </summary>
    public string CorrectOption { get; set; } = string.Empty;
}
