using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.ListExams;

public record ListExamsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? ExamTypeId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    ExamStatus? Status = null
);

public record ExamDto(
    Guid Id,
    string Code,
    string Title,
    Guid ExamTypeId,
    string ExamTypeName,
    DateTimeOffset ExamDate,
    ExamStatus Status,
    int SectionCount
);

public record PaginatedList<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public class ListExamsQueryValidator : AbstractValidator<ListExamsQuery>
{
    public ListExamsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class ListExamsHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IAcademicCatalog academicCatalog)
{
    public async Task<PaginatedList<ExamDto>> HandleAsync(ListExamsQuery query, CancellationToken ct = default)
    {
        await new ListExamsQueryValidator().ValidateAndThrowAsync(query, ct);

        var exams = db.Set<Exam>().AsNoTracking();

        if (!currentUser.IsSuperAdmin)
        {
            var institutionId = await currentUser.GetInstitutionIdAsync();
            if (!institutionId.HasValue)
                throw new ForbiddenException("Institution context required.");
            exams = exams.Where(e => e.InstitutionId == institutionId.Value);
        }

        if (query.ExamTypeId.HasValue)
            exams = exams.Where(e => e.ExamTypeId == query.ExamTypeId.Value);

        if (query.From.HasValue)
            exams = exams.Where(e => e.ExamDate >= query.From.Value);

        if (query.To.HasValue)
            exams = exams.Where(e => e.ExamDate <= query.To.Value);

        if (query.Status.HasValue)
            exams = exams.Where(e => e.Status == query.Status.Value);

        var totalCount = await exams.CountAsync(ct);

        var pagedExams = await exams
            .OrderByDescending(e => e.ExamDate)
            .ThenBy(e => e.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new
            {
                e.Id,
                e.Code,
                e.Title,
                e.ExamTypeId,
                e.ExamDate,
                e.Status,
                SectionCount = db.Set<ExamSection>().Count(s => s.ExamId == e.Id)
            })
            .ToListAsync(ct);

        var examTypeIds = pagedExams.Select(e => e.ExamTypeId).Distinct();
        var examTypes = await academicCatalog.GetExamTypesAsync(examTypeIds, ct);
        var examTypeDict = examTypes.ToDictionary(x => x.Id, x => x.Name);

        var items = pagedExams.Select(e => new ExamDto(
            e.Id,
            e.Code,
            e.Title,
            e.ExamTypeId,
            examTypeDict.GetValueOrDefault(e.ExamTypeId, "Unknown"),
            e.ExamDate,
            e.Status,
            e.SectionCount
        )).ToList();

        return new PaginatedList<ExamDto>(items, totalCount, query.Page, query.PageSize);
    }
}
