namespace EgitimPlatform.BuildingBlocks.Interfaces;

/// <summary>
/// Cross-module query for coach → student assignment lookups.
/// Defined in BuildingBlocks to avoid circular module references.
/// Implemented by the Coaching module.
/// </summary>
public interface ICoachStudentQuery
{
    /// <summary>
    /// Gets IDs of students actively assigned to a coach.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetActiveAssignedStudentIdsAsync(Guid coachId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a coach has an active assignment to a specific student.
    /// </summary>
    Task<bool> HasActiveAssignmentAsync(Guid coachId, Guid studentId, CancellationToken ct = default);
}
