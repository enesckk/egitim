using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Exams.Entities;

public sealed class ExamSection : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid ExamId { get; set; }
    public Guid SubjectId { get; set; }
    public int DisplayOrder { get; set; }
    public int QuestionCount { get; set; }
    public decimal? WrongAnswerPenalty { get; set; }
}
