using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Students.Entities;

/// <summary>
/// Sprint 2 — Student academic goal.
/// Represents a target the student is working toward (exam, score, school, etc.).
/// Goal changes are tracked immutably via StudentGoalHistory.
/// </summary>
public class StudentGoal : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid StudentId { get; set; }

    /// <summary>Short title — e.g. "YKS 2027 Sayısal".</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional description / notes about the goal.</summary>
    public string? Description { get; set; }

    /// <summary>Target exam type (FK to Academic.ExamType when taxonomy exists). Null until taxonomy module available.</summary>
    public Guid? TargetExamTypeId { get; set; }

    /// <summary>Target score (exam-specific). Null if not applicable.</summary>
    public int? TargetScore { get; set; }

    /// <summary>Target rank. Null if not applicable.</summary>
    public int? TargetRank { get; set; }

    /// <summary>Target school/program name. Free text.</summary>
    public string? TargetSchoolName { get; set; }

    /// <summary>Date from which this goal is effective.</summary>
    public DateTimeOffset EffectiveDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Whether this goal is currently active. Deactivation preserves history.</summary>
    public bool IsActive { get; set; } = true;
}
