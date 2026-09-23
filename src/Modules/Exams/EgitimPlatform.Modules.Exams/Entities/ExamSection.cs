using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Exams.Entities;

/// <summary>
/// Sprint 3 — Subject section within an Exam (e.g. Matematik section in a TYT exam).
/// </summary>
public class ExamSection : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid ExamId { get; set; }
    public Guid SubjectId { get; set; }

    public int DisplayOrder { get; set; }
    public int QuestionCount { get; set; }

    /// <summary>Optional penalty override for this section. If null, exam penalty applies.</summary>
    public decimal? WrongAnswerPenalty { get; set; }
}
