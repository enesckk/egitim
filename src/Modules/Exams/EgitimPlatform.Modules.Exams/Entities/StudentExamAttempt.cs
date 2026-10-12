using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Exams.Entities;

public sealed class StudentExamAttempt : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }
    public int AttemptNumber { get; set; } = 1;
    public DateTimeOffset TakenAt { get; set; }
    public ExamAttemptStatus Status { get; set; } = ExamAttemptStatus.Draft;
    public int TotalCorrect { get; set; }
    public int TotalWrong { get; set; }
    public int TotalBlank { get; set; }
    public decimal TotalNet { get; set; }
    public decimal? ReportedScore { get; set; }
    public ScoreSource ScoreSource { get; set; } = ScoreSource.None;
    public DateTimeOffset? FinalizedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
