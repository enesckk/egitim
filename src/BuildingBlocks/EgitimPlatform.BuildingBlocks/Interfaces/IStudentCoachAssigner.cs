namespace EgitimPlatform.BuildingBlocks.Interfaces;

/// <summary>
/// Cross-module command for creating coach-student assignments.
/// Defined in BuildingBlocks to avoid circular module references.
/// Implemented by the Coaching module.
/// </summary>
public interface IStudentCoachAssigner
{
    /// <summary>
    /// Creates an active primary assignment between a student and coach.
    /// Does NOT call SaveChanges — caller is responsible for the transaction boundary.
    /// </summary>
    void CreatePrimaryAssignment(Guid studentId, Guid coachId, Guid institutionId, Guid? createdBy);

    /// <summary>
    /// Finds a coach entity ID by the user's identity ID.
    /// Returns null if no coach profile exists for this user.
    /// </summary>
    Task<Guid?> FindCoachIdByUserIdAsync(Guid userId, CancellationToken ct = default);
}
