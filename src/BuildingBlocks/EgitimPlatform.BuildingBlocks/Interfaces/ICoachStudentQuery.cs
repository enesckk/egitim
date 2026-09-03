namespace EgitimPlatform.BuildingBlocks.Interfaces;

/// <summary>
/// Cross-module query for coach → student assignment lookups.
/// Defined in BuildingBlocks to avoid circular module references.
/// Implemented by the Coaching module.
///
/// P1-01 FIX: Methods accept the authenticated user's userId (ApplicationUser.Id),
/// NOT Coach.Id. The implementation resolves userId → Coach.Id internally,
/// preventing callers from accidentally passing ApplicationUser.Id where Coach.Id
/// is expected (these are different IDs).
/// </summary>
public interface ICoachStudentQuery
{
    /// <summary>
    /// Gets IDs of students actively assigned to the coach identified by userId.
    /// Returns empty list if no coach profile exists for this user.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetActiveAssignedStudentIdsByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the coach identified by userId has an active assignment to a specific student.
    /// Returns false if no coach profile exists for this user.
    /// </summary>
    Task<bool> HasActiveAssignmentByUserAsync(Guid userId, Guid studentId, CancellationToken ct = default);
}
