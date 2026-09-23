using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.GetStudentAnalysis;

public record GetStudentAnalysisQuery(
    Guid? ExamTypeId = null,
    int Window = 10
);

public record SubjectAverageDto(
    Guid SubjectId,
    string SubjectName,
    decimal AverageNet,
    decimal AverageCorrect,
    decimal AverageWrong
);

public record WeakTopicDto(
    Guid TopicId,
    string TopicName,
    Guid SubjectId,
    string SubjectName,
    int TotalAttempts,
    decimal AverageNet,
    decimal WrongRatio
);

public record StudentExamAnalysisDto(
    int AttemptCount,
    decimal AverageTotalNet,
    decimal LatestTotalNet,
    decimal PreviousTotalNet,
    decimal Delta,
    List<SubjectAverageDto> SubjectAverages,
    List<WeakTopicDto> WeakestTopics
);

public class GetStudentAnalysisQueryValidator : AbstractValidator<GetStudentAnalysisQuery>
{
    public GetStudentAnalysisQueryValidator()
    {
        RuleFor(x => x.Window).InclusiveBetween(1, 50);
    }
}

public class GetStudentAnalysisHandler(
    IApplicationDbContext db,
    IExamResourceAccess resourceAccess,
    IAcademicCatalog academicCatalog)
{
    public async Task<StudentExamAnalysisDto> HandleAsync(
        Guid studentId,
        GetStudentAnalysisQuery query,
        CancellationToken ct = default)
    {
        await new GetStudentAnalysisQueryValidator().ValidateAndThrowAsync(query, ct);

        await resourceAccess.ValidateStudentReadAccessAsync(studentId, ct);

        var attemptsQuery = db.Set<StudentExamAttempt>()
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && a.Status == AttemptStatus.Finalized);

        if (query.ExamTypeId.HasValue)
        {
            attemptsQuery = from a in attemptsQuery
                            join e in db.Set<Exam>().AsNoTracking() on a.ExamId equals e.Id
                            where e.ExamTypeId == query.ExamTypeId.Value
                            select a;
        }

        var attempts = await attemptsQuery
            .OrderByDescending(a => a.TakenAt)
            .ThenBy(a => a.Id)
            .Take(query.Window)
            .ToListAsync(ct);

        if (attempts.Count == 0)
        {
            return new StudentExamAnalysisDto(
                AttemptCount: 0,
                AverageTotalNet: 0m,
                LatestTotalNet: 0m,
                PreviousTotalNet: 0m,
                Delta: 0m,
                SubjectAverages: [],
                WeakestTopics: []
            );
        }

        var attemptIds = attempts.Select(a => a.Id).ToList();

        var latestNet = attempts[0].TotalNet;
        var previousNet = attempts.Count > 1 ? attempts[1].TotalNet : 0m;
        var delta = attempts.Count > 1 ? Math.Round(latestNet - previousNet, 2, MidpointRounding.AwayFromZero) : 0m;
        var avgNet = Math.Round(attempts.Average(a => a.TotalNet), 2, MidpointRounding.AwayFromZero);

        // Subject averages
        var subjectResults = await (from sr in db.Set<StudentExamSubjectResult>().AsNoTracking()
                                    join sec in db.Set<ExamSection>().AsNoTracking() on sr.ExamSectionId equals sec.Id
                                    where attemptIds.Contains(sr.StudentExamAttemptId)
                                    select new { sr.StudentExamAttemptId, sec.SubjectId, sr.Correct, sr.Wrong, sr.Net })
                                   .ToListAsync(ct);

        var subjectGroups = subjectResults.GroupBy(x => x.SubjectId).ToList();
        var subjectIds = subjectGroups.Select(g => g.Key);
        var subjectNames = await academicCatalog.GetSubjectNamesAsync(subjectIds, ct);

        var subjectAverages = subjectGroups.Select(g => new SubjectAverageDto(
            SubjectId: g.Key,
            SubjectName: subjectNames.GetValueOrDefault(g.Key, "Unknown Subject"),
            AverageNet: Math.Round(g.Average(x => x.Net), 2, MidpointRounding.AwayFromZero),
            AverageCorrect: Math.Round((decimal)g.Average(x => x.Correct), 2, MidpointRounding.AwayFromZero),
            AverageWrong: Math.Round((decimal)g.Average(x => x.Wrong), 2, MidpointRounding.AwayFromZero)
        )).OrderBy(s => s.SubjectName).ToList();

        // Weak topics
        var topicResults = await (from tr in db.Set<StudentExamTopicResult>().AsNoTracking()
                                  join sec in db.Set<ExamSection>().AsNoTracking() on tr.ExamSectionId equals sec.Id
                                  where attemptIds.Contains(tr.StudentExamAttemptId)
                                  select new { tr.TopicId, sec.SubjectId, tr.Correct, tr.Wrong, tr.Blank, tr.Net })
                                 .ToListAsync(ct);

        var topicGroups = topicResults.GroupBy(x => new { x.TopicId, x.SubjectId }).ToList();
        var topicIds = topicGroups.Select(g => g.Key.TopicId);
        var topicNames = await academicCatalog.GetTopicNamesAsync(topicIds, ct);

        var weakTopics = topicGroups.Select(g =>
        {
            var totalQ = g.Sum(x => x.Correct + x.Wrong + x.Blank);
            var wrongRatio = totalQ > 0 ? Math.Round((decimal)g.Sum(x => x.Wrong) / totalQ, 2, MidpointRounding.AwayFromZero) : 0m;
            var avgTopicNet = Math.Round(g.Average(x => x.Net), 2, MidpointRounding.AwayFromZero);

            return new WeakTopicDto(
                TopicId: g.Key.TopicId,
                TopicName: topicNames.GetValueOrDefault(g.Key.TopicId, "Unknown Topic"),
                SubjectId: g.Key.SubjectId,
                SubjectName: subjectNames.GetValueOrDefault(g.Key.SubjectId, "Unknown Subject"),
                TotalAttempts: g.Count(),
                AverageNet: avgTopicNet,
                WrongRatio: wrongRatio
            );
        })
        .OrderBy(t => t.AverageNet)
        .ThenByDescending(t => t.WrongRatio)
        .Take(10)
        .ToList();

        return new StudentExamAnalysisDto(
            AttemptCount: attempts.Count,
            AverageTotalNet: avgNet,
            LatestTotalNet: latestNet,
            PreviousTotalNet: previousNet,
            Delta: delta,
            SubjectAverages: subjectAverages,
            WeakestTopics: weakTopics
        );
    }
}
