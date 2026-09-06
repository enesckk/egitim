namespace EgitimPlatform.BuildingBlocks.Interfaces;
public record StudentCoachSummaryDto(Guid CoachId, string Name, bool IsPrimary, DateTimeOffset AssignedAt);
/// <summary>Internal composition contract. Caller must authorize the student first.</summary>
public interface IStudentCoachSummaryQuery
{
    Task<IReadOnlyList<StudentCoachSummaryDto>> GetAsync(Guid studentId, Guid institutionId, CancellationToken ct = default);
}
