namespace EgitimPlatform.BuildingBlocks.Interfaces;

public record StudentReferenceDto(Guid Id, Guid InstitutionId, Guid? UserId);

public record StudentImportLookupDto(Guid Id, Guid InstitutionId, string? StudentNumber, string FirstName, string LastName);

public record StudentSummaryDto(Guid Id, Guid InstitutionId, string FirstName, string LastName, string? StudentNumber, int? GradeLevel);

public interface IStudentDirectory
{
    Task<StudentReferenceDto?> GetStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<bool> IsParentRelatedToStudentAsync(Guid parentUserId, Guid studentId, Guid institutionId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentImportLookupDto>> GetActiveStudentsForImportAsync(Guid institutionId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentSummaryDto>> GetStudentsByIdsAsync(IEnumerable<Guid> studentIds, Guid institutionId, CancellationToken ct = default);
}
