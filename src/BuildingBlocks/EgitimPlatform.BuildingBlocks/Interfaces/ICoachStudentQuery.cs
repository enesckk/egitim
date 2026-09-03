namespace EgitimPlatform.BuildingBlocks.Interfaces;

/// <summary>
/// Cross-module query for coach → student assignment lookups.
/// Defined in BuildingBlocks to avoid circular module references.
/// Implemented by the Coaching module.
///
/// P1-01 CLOSURE: All methods require BOTH applicationUserId AND institutionId.
/// The implementation resolves (userId, institutionId) → Coach.Id with institution-scoped
/// lookup, preventing authorization ambiguity when a user could have multiple coach profiles.
///
/// Canonical resolution path:
/// authenticated ApplicationUser
///     → ApplicationUser.Id + ApplicationUser.InstitutionId
///     → Coach.UserId + Coach.InstitutionId
///     → Coach.Id
///     → active StudentCoachAssignment (same institution)
///     → Student
///
/// Institution-less users FAIL CLOSED (no profile resolution).
/// </summary>
public interface ICoachStudentQuery
{
    /// <summary>
    /// Gets IDs of students actively assigned to the coach identified by
    /// (applicationUserId, institutionId).
    /// Returns empty list if no coach profile exists for this user in this institution.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetActiveAssignedStudentIdsAsync(
        Guid applicationUserId, Guid institutionId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the coach identified by (applicationUserId, institutionId) has an
    /// active assignment to a specific student.
    /// The student must also belong to the same institution.
    /// Returns false if no coach profile exists for this user in this institution.
    /// </summary>
    Task<bool> HasActiveAssignmentAsync(
        Guid applicationUserId, Guid institutionId, Guid studentId, CancellationToken ct = default);
}
