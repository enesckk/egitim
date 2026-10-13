using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Exams.Entities;

/// <summary>
/// Sprint 3 — Exam (Deneme Sınavı) definition.
/// Shared reference exam template within an institution.
/// </summary>
public class Exam : SoftDeletableEntity, IHasInstitutionId
{
    public byte[] RowVersion { get; set; } = [];
    public Guid InstitutionId { get; set; }
    public Guid ExamTypeId { get; set; }

    /// <summary>Globally unique code within active institution exams (e.g. "TYT-2027-01").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable title (e.g. "Genel Deneme Sınavı 1").</summary>
    public string Title { get; set; } = string.Empty;

    public DateTimeOffset ExamDate { get; set; } = DateTimeOffset.UtcNow;
    public ExamStatus Status { get; set; } = ExamStatus.Draft;
    public decimal WrongAnswerPenalty { get; set; } = 4m;
    public ScoreSource ScoreSource { get; set; } = ScoreSource.None;

    public List<ExamSection> Sections { get; set; } = [];
}
