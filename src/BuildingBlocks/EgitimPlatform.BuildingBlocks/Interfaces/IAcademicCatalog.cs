namespace EgitimPlatform.BuildingBlocks.Interfaces;

public record AcademicReferenceDto(Guid Id, string Code, string Name);

public interface IAcademicCatalog
{
    Task<AcademicReferenceDto?> GetExamTypeAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AcademicReferenceDto>> GetExamTypesAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<bool> SubjectExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> ValidateExamTypeSubjectAsync(Guid examTypeId, Guid subjectId, CancellationToken ct = default);
    Task<bool> ValidateSubjectTopicsAsync(Guid subjectId, IEnumerable<Guid> topicIds, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string>> GetSubjectNamesAsync(IEnumerable<Guid> subjectIds, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string>> GetTopicNamesAsync(IEnumerable<Guid> topicIds, CancellationToken ct = default);
}

