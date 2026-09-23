using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Exams.Entities;

/// <summary>
/// Sprint 3 — Student attempt for a specific Exam.
/// Holds aggregate totals and transitions from Draft to Finalized.
/// </summary>
public class StudentExamAttempt : SoftDeletableEntity, IHasInstitutionId
{
    public byte[] RowVersion { get; set; } = [];
    public Guid InstitutionId { get; set; }
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }

    public int AttemptNumber { get; set; } = 1;
    public DateTimeOffset TakenAt { get; set; } = DateTimeOffset.UtcNow;
    public AttemptStatus Status { get; set; } = AttemptStatus.Draft;

    public int TotalCorrect { get; set; }
    public int TotalWrong { get; set; }
    public int TotalBlank { get; set; }
    public decimal TotalNet { get; set; }

    public decimal? ReportedScore { get; set; }
    public ScoreSource ScoreSource { get; set; } = ScoreSource.None;
    public DateTimeOffset? FinalizedAt { get; set; }

    public List<StudentExamSubjectResult> SubjectResults { get; set; } = [];
    public List<StudentExamTopicResult> TopicResults { get; set; } = [];
}
