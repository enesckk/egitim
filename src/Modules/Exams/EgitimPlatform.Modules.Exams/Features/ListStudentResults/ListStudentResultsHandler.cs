using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.ListExams;
using EgitimPlatform.Modules.Exams.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.ListStudentResults;

public record ListStudentResultsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? ExamTypeId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    bool OnlyFinalized = true
);

public record StudentExamResultSummaryDto(
    Guid AttemptId,
    Guid ExamId,
    string ExamTitle,
    Guid ExamTypeId,
    string ExamTypeName,
    DateTimeOffset TakenAt,
    AttemptStatus Status,
    int TotalCorrect,
    int TotalWrong,
    int TotalBlank,
    decimal TotalNet,
    decimal? ReportedScore,
    ScoreSource ScoreSource
);

public class ListStudentResultsQueryValidator : AbstractValidator<ListStudentResultsQuery>
{
    public ListStudentResultsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class ListStudentResultsHandler(
    IApplicationDbContext db,
    IExamResourceAccess resourceAccess,
    IAcademicCatalog academicCatalog)
{
    public async Task<PaginatedList<StudentExamResultSummaryDto>> HandleAsync(
        Guid studentId,
        ListStudentResultsQuery query,
        CancellationToken ct = default)
    {
        await new ListStudentResultsQueryValidator().ValidateAndThrowAsync(query, ct);

        await resourceAccess.ValidateStudentReadAccessAsync(studentId, ct);

        var attempts = db.Set<StudentExamAttempt>()
            .AsNoTracking()
            .Where(a => a.StudentId == studentId);

        if (query.OnlyFinalized)
            attempts = attempts.Where(a => a.Status == AttemptStatus.Finalized);

        if (query.From.HasValue)
            attempts = attempts.Where(a => a.TakenAt >= query.From.Value);

        if (query.To.HasValue)
            attempts = attempts.Where(a => a.TakenAt <= query.To.Value);

        var queryJoined = from a in attempts
                          join e in db.Set<Exam>().AsNoTracking() on a.ExamId equals e.Id
                          select new { Attempt = a, Exam = e };

        if (query.ExamTypeId.HasValue)
            queryJoined = queryJoined.Where(x => x.Exam.ExamTypeId == query.ExamTypeId.Value);

        var totalCount = await queryJoined.CountAsync(ct);

        var pagedData = await queryJoined
            .OrderByDescending(x => x.Attempt.TakenAt)
            .ThenBy(x => x.Attempt.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new
            {
                AttemptId = x.Attempt.Id,
                ExamId = x.Exam.Id,
                ExamTitle = x.Exam.Title,
                ExamTypeId = x.Exam.ExamTypeId,
                TakenAt = x.Attempt.TakenAt,
                Status = x.Attempt.Status,
                TotalCorrect = x.Attempt.TotalCorrect,
                TotalWrong = x.Attempt.TotalWrong,
                TotalBlank = x.Attempt.TotalBlank,
                TotalNet = x.Attempt.TotalNet,
                ReportedScore = x.Attempt.ReportedScore,
                ScoreSource = x.Attempt.ScoreSource
            })
            .ToListAsync(ct);

        var examTypeIds = pagedData.Select(x => x.ExamTypeId).Distinct();
        var examTypes = await academicCatalog.GetExamTypesAsync(examTypeIds, ct);
        var examTypeDict = examTypes.ToDictionary(x => x.Id, x => x.Name);

        var items = pagedData.Select(x => new StudentExamResultSummaryDto(
            x.AttemptId,
            x.ExamId,
            x.ExamTitle,
            x.ExamTypeId,
            examTypeDict.GetValueOrDefault(x.ExamTypeId, "Unknown"),
            x.TakenAt,
            x.Status,
            x.TotalCorrect,
            x.TotalWrong,
            x.TotalBlank,
            x.TotalNet,
            x.ReportedScore,
            x.ScoreSource
        )).ToList();

        return new PaginatedList<StudentExamResultSummaryDto>(items, totalCount, query.Page, query.PageSize);
    }
}
