using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Exams.Entities;

public sealed class StudentExamSubjectResult : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid ExamId { get; set; }
    public Guid StudentExamAttemptId { get; set; }
    public Guid ExamSectionId { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public int Blank { get; set; }
    public decimal Net { get; set; }
}
