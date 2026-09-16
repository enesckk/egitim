using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.Modules.Academic.Entities;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
namespace EgitimPlatform.Modules.Academic.Seeding;
public record TaxonomySeedRecord(string Level, Guid Id, string Code, string Name, Guid? ParentId);
public class TaxonomySeedValidator : AbstractValidator<TaxonomySeedRecord>
{
    public TaxonomySeedValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100).Matches("^[A-Z0-9][A-Z0-9._-]*$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Level).Must(x => new[] { "ExamType", "Subject", "Topic", "SubTopic", "LearningOutcome", "Concept" }.Contains(x));
        RuleFor(x => x.ParentId).Null().When(x => x.Level == "ExamType");
        RuleFor(x => x.ParentId).NotNull().NotEqual(Guid.Empty).When(x => x.Level != "ExamType");
    }
}
/// <summary>Explicit initialization only. Input must come from an independently verified manifest.
/// No default curriculum is supplied. One SaveChanges keeps the manifest atomic.</summary>
public class TaxonomySeeder(IApplicationDbContext db)
{
    public async Task SeedAsync(IReadOnlyList<TaxonomySeedRecord> records, CancellationToken ct = default)
    {
        foreach (var r in records) await new TaxonomySeedValidator().ValidateAndThrowAsync(r, ct);
        if (records.GroupBy(x => x.Id).Any(x => x.Count() > 1) || records.GroupBy(x => (x.Level, x.Code)).Any(x => x.Count() > 1))
            throw new ValidationException("Duplicate manifest identifier or code.");
        foreach (var r in records.Where(x => x.Level == "ExamType"))
        {
            var existing = await db.Set<ExamType>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == r.Id || x.Code == r.Code, ct);
            if (existing is not null)
            {
                if (existing.Id != r.Id || existing.Code != r.Code || existing.Name != r.Name || existing.IsDeleted)
                    throw new ConflictException("Manifest conflicts with existing reference data; explicit review required.");
                continue;
            }
            db.Set<ExamType>().Add(new ExamType { Id = r.Id, Code = r.Code, Name = r.Name });
        }
        foreach (var r in records.Where(x => x.Level == "Subject"))
        {
            var existing = await db.Set<Subject>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == r.Id || x.Code == r.Code, ct);
            if (existing is not null)
            {
                if (existing.Id != r.Id || existing.Code != r.Code || existing.Name != r.Name || existing.IsDeleted || existing.ExamTypeId != r.ParentId)
                    throw new ConflictException("Manifest conflicts with existing reference data; explicit review required.");
                continue;
            }
            if (!db.Set<ExamType>().Local.Any(x => x.Id == r.ParentId && !x.IsDeleted) &&
                !await db.Set<ExamType>().AnyAsync(x => x.Id == r.ParentId, ct))
                throw new ValidationException("Unknown parent reference.");
            db.Set<Subject>().Add(new Subject { Id = r.Id, Code = r.Code, Name = r.Name, ExamTypeId = r.ParentId!.Value });
        }
        foreach (var r in records.Where(x => x.Level == "Topic"))
        {
            var existing = await db.Set<Topic>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == r.Id || x.Code == r.Code, ct);
            if (existing is not null)
            {
                if (existing.Id != r.Id || existing.Code != r.Code || existing.Name != r.Name || existing.IsDeleted || existing.SubjectId != r.ParentId)
                    throw new ConflictException("Manifest conflicts with existing reference data; explicit review required.");
                continue;
            }
            if (!db.Set<Subject>().Local.Any(x => x.Id == r.ParentId && !x.IsDeleted) &&
                !await db.Set<Subject>().AnyAsync(x => x.Id == r.ParentId, ct))
                throw new ValidationException("Unknown parent reference.");
            db.Set<Topic>().Add(new Topic { Id = r.Id, Code = r.Code, Name = r.Name, SubjectId = r.ParentId!.Value });
        }
        foreach (var r in records.Where(x => x.Level == "SubTopic"))
        {
            var existing = await db.Set<SubTopic>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == r.Id || x.Code == r.Code, ct);
            if (existing is not null)
            {
                if (existing.Id != r.Id || existing.Code != r.Code || existing.Name != r.Name || existing.IsDeleted || existing.TopicId != r.ParentId)
                    throw new ConflictException("Manifest conflicts with existing reference data; explicit review required.");
                continue;
            }
            if (!db.Set<Topic>().Local.Any(x => x.Id == r.ParentId && !x.IsDeleted) &&
                !await db.Set<Topic>().AnyAsync(x => x.Id == r.ParentId, ct))
                throw new ValidationException("Unknown parent reference.");
            db.Set<SubTopic>().Add(new SubTopic { Id = r.Id, Code = r.Code, Name = r.Name, TopicId = r.ParentId!.Value });
        }
        foreach (var r in records.Where(x => x.Level == "LearningOutcome"))
        {
            var existing = await db.Set<LearningOutcome>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == r.Id || x.Code == r.Code, ct);
            if (existing is not null)
            {
                if (existing.Id != r.Id || existing.Code != r.Code || existing.Name != r.Name || existing.IsDeleted || existing.SubTopicId != r.ParentId)
                    throw new ConflictException("Manifest conflicts with existing reference data; explicit review required.");
                continue;
            }
            if (!db.Set<SubTopic>().Local.Any(x => x.Id == r.ParentId && !x.IsDeleted) &&
                !await db.Set<SubTopic>().AnyAsync(x => x.Id == r.ParentId, ct))
                throw new ValidationException("Unknown parent reference.");
            db.Set<LearningOutcome>().Add(new LearningOutcome { Id = r.Id, Code = r.Code, Name = r.Name, SubTopicId = r.ParentId!.Value });
        }
        foreach (var r in records.Where(x => x.Level == "Concept"))
        {
            var existing = await db.Set<Concept>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == r.Id || x.Code == r.Code, ct);
            if (existing is not null)
            {
                if (existing.Id != r.Id || existing.Code != r.Code || existing.Name != r.Name || existing.IsDeleted || existing.LearningOutcomeId != r.ParentId)
                    throw new ConflictException("Manifest conflicts with existing reference data; explicit review required.");
                continue;
            }
            if (!db.Set<LearningOutcome>().Local.Any(x => x.Id == r.ParentId && !x.IsDeleted) &&
                !await db.Set<LearningOutcome>().AnyAsync(x => x.Id == r.ParentId, ct))
                throw new ValidationException("Unknown parent reference.");
            db.Set<Concept>().Add(new Concept { Id = r.Id, Code = r.Code, Name = r.Name, LearningOutcomeId = r.ParentId!.Value });
        }
        await db.SaveChangesAsync(ct);
    }
}
