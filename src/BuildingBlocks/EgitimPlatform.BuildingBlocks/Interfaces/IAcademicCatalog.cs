namespace EgitimPlatform.BuildingBlocks.Interfaces;
public record AcademicReferenceDto(Guid Id, string Code, string Name);
public interface IAcademicCatalog
{
    Task<AcademicReferenceDto?> GetExamTypeAsync(Guid id, CancellationToken ct = default);
    Task<bool> SubjectExistsAsync(Guid id, CancellationToken ct = default);
}
