using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Exams.Entities;

/// <summary>
/// Sprint 3 — Topic level performance aggregate for a student's attempt section.
/// </summary>
public class StudentExamTopicResult : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid StudentExamAttemptId { get; set; }
    public Guid ExamSectionId { get; set; }
    public Guid TopicId { get; set; }

    public int Correct { get; set; }
    public int Wrong { get; set; }
    public int Blank { get; set; }
    public decimal Net { get; set; }
}
