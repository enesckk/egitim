using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Exams.Entities;

public sealed class Exam : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid ExamTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset ExamDate { get; set; }
    public ExamStatus Status { get; set; } = ExamStatus.Draft;
    public decimal WrongAnswerPenalty { get; set; } = 4m;
    public ScoreSource ScoreSource { get; set; } = ScoreSource.None;
    public byte[] RowVersion { get; set; } = [];
}
