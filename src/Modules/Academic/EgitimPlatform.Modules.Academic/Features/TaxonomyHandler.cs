using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Academic.Entities;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
namespace EgitimPlatform.Modules.Academic.Features;
public sealed class TaxonomyHandler(IApplicationDbContext db) : IAcademicCatalog
{
    private IQueryable<ExamType> ExamTypes => db.Set<ExamType>().AsNoTracking();
    private IQueryable<Subject> Subjects => db.Set<Subject>().AsNoTracking().Where(x => ExamTypes.Select(p => p.Id).Contains(x.ExamTypeId));
    private IQueryable<Topic> Topics => db.Set<Topic>().AsNoTracking().Where(x => Subjects.Select(p => p.Id).Contains(x.SubjectId));
    private IQueryable<SubTopic> SubTopics => db.Set<SubTopic>().AsNoTracking().Where(x => Topics.Select(p => p.Id).Contains(x.TopicId));
    private IQueryable<LearningOutcome> LearningOutcomes => db.Set<LearningOutcome>().AsNoTracking().Where(x => SubTopics.Select(p => p.Id).Contains(x.SubTopicId));
    private IQueryable<Concept> Concepts => db.Set<Concept>().AsNoTracking().Where(x => LearningOutcomes.Select(p => p.Id).Contains(x.LearningOutcomeId));
    public async Task<AcademicReferenceDto?> GetExamTypeAsync(Guid id, CancellationToken ct = default) =>
        await ExamTypes.Where(x => x.Id == id).Select(x => new AcademicReferenceDto(x.Id, x.Code, x.Name)).SingleOrDefaultAsync(ct);
    public async Task<IReadOnlyList<AcademicReferenceDto>> GetExamTypesAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return Array.Empty<AcademicReferenceDto>();
        return await ExamTypes.Where(x => idList.Contains(x.Id)).OrderBy(x => x.Code)
            .Select(x => new AcademicReferenceDto(x.Id, x.Code, x.Name)).ToListAsync(ct);
    }
    public Task<bool> SubjectExistsAsync(Guid id, CancellationToken ct = default) => Subjects.AnyAsync(x => x.Id == id, ct);
    public Task<bool> ValidateExamTypeSubjectAsync(Guid examTypeId, Guid subjectId, CancellationToken ct = default) =>
        Subjects.AnyAsync(x => x.Id == subjectId && x.ExamTypeId == examTypeId, ct);
    public async Task<bool> ValidateSubjectTopicsAsync(Guid subjectId, IEnumerable<Guid> topicIds, CancellationToken ct = default)
    {
        var idList = topicIds.Distinct().ToList();
        if (idList.Count == 0) return true;
        var validCount = await Topics.Where(x => x.SubjectId == subjectId && idList.Contains(x.Id)).CountAsync(ct);
        return validCount == idList.Count;
    }
    public async Task<IReadOnlyDictionary<Guid, string>> GetSubjectNamesAsync(IEnumerable<Guid> subjectIds, CancellationToken ct = default)
    {
        var idList = subjectIds.Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<Guid, string>();
        return await Subjects.Where(x => idList.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
    }
    public async Task<IReadOnlyDictionary<Guid, string>> GetTopicNamesAsync(IEnumerable<Guid> topicIds, CancellationToken ct = default)
    {
        var idList = topicIds.Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<Guid, string>();
        return await Topics.Where(x => idList.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
    }
    public async Task<IReadOnlyList<TaxonomyDto>> HandleAsync(TaxonomyQuery q, CancellationToken ct = default)
    {
        await new TaxonomyQueryValidator().ValidateAndThrowAsync(q, ct);
        IQueryable<TaxonomyDto> source = q.Level switch
        {
            "exam-types" => ExamTypes.Where(x => !q.ParentId.HasValue).OrderBy(x => x.Code).Select(x => new TaxonomyDto(x.Id, x.Code, x.Name, null)),
            "subjects" => Subjects.Where(x => !q.ParentId.HasValue || x.ExamTypeId == q.ParentId).OrderBy(x => x.Code).Select(x => new TaxonomyDto(x.Id, x.Code, x.Name, x.ExamTypeId)),
            "topics" => Topics.Where(x => !q.ParentId.HasValue || x.SubjectId == q.ParentId).OrderBy(x => x.Code).Select(x => new TaxonomyDto(x.Id, x.Code, x.Name, x.SubjectId)),
            "sub-topics" => SubTopics.Where(x => !q.ParentId.HasValue || x.TopicId == q.ParentId).OrderBy(x => x.Code).Select(x => new TaxonomyDto(x.Id, x.Code, x.Name, x.TopicId)),
            "learning-outcomes" => LearningOutcomes.Where(x => !q.ParentId.HasValue || x.SubTopicId == q.ParentId).OrderBy(x => x.Code).Select(x => new TaxonomyDto(x.Id, x.Code, x.Name, x.SubTopicId)),
            "concepts" => Concepts.Where(x => !q.ParentId.HasValue || x.LearningOutcomeId == q.ParentId).OrderBy(x => x.Code).Select(x => new TaxonomyDto(x.Id, x.Code, x.Name, x.LearningOutcomeId)),
            _ => throw new ValidationException("Unknown taxonomy level.")
        };
        return await source.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);
    }
}
