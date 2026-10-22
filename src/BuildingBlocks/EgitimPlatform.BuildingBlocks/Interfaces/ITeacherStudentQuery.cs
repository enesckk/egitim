namespace EgitimPlatform.BuildingBlocks.Interfaces;

/// <summary>
/// Cross-module query for teacher → student classroom assignment lookups.
/// Defined in BuildingBlocks to avoid circular module references.
/// Implemented by the Teachers module.
/// </summary>
public interface ITeacherStudentQuery
{
    /// <summary>
    /// Gets IDs of students actively assigned to the teacher identified by
    /// (applicationUserId, institutionId) through active classrooms.
    /// Returns empty list if no teacher profile or classroom assignments exist.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetActiveAssignedStudentIdsAsync(
        Guid applicationUserId, Guid institutionId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the teacher identified by (applicationUserId, institutionId) has an
    /// active classroom assignment that includes the specified student.
    /// Returns false if no teacher profile exists or student is not in teacher's classrooms.
    /// </summary>
    Task<bool> HasActiveAssignmentAsync(
        Guid applicationUserId, Guid institutionId, Guid studentId, CancellationToken ct = default);
}
