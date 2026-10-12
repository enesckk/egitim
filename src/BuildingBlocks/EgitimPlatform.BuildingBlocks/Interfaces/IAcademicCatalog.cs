namespace EgitimPlatform.BuildingBlocks.Interfaces;
public record AcademicReferenceDto(Guid Id, string Code, string Name);
public record AcademicSubjectReferenceDto(Guid Id, Guid ExamTypeId, string Code, string Name);
public record AcademicTopicReferenceDto(Guid Id, Guid SubjectId, string Code, string Name);
public interface IAcademicCatalog
{
    Task<AcademicReferenceDto?> GetExamTypeAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AcademicReferenceDto>> GetExamTypesAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<bool> SubjectExistsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AcademicSubjectReferenceDto>> GetSubjectsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<AcademicTopicReferenceDto>> GetTopicsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}
