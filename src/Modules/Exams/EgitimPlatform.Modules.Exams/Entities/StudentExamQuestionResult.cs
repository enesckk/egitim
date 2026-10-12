using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Exams.Entities;

/// <summary>
/// Sprint 4A — Raw question-level result record for a student exam attempt.
/// </summary>
public class StudentExamQuestionResult : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid StudentExamAttemptId { get; set; }
    public Guid ExamQuestionId { get; set; }
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Student selected option label (e.g. "A"). Null if blank.
    /// </summary>
    public string? SelectedOption { get; set; }

    public bool? IsCorrect { get; set; }
    public bool IsBlank { get; set; } = true;
    public decimal NetContribution { get; set; }
}
