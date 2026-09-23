using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.GetResultDetail;

public record TopicResultDetailDto(
    Guid Id,
    Guid TopicId,
    string TopicName,
    int Correct,
    int Wrong,
    int Blank,
    decimal Net
);

public record SubjectResultDetailDto(
    Guid Id,
    Guid ExamSectionId,
    Guid SubjectId,
    string SubjectName,
    int Correct,
    int Wrong,
    int Blank,
    decimal Net,
    List<TopicResultDetailDto> Topics
);

public record ExamResultDetailDto(
    Guid AttemptId,
    Guid ExamId,
    string ExamTitle,
    string ExamCode,
    Guid ExamTypeId,
    string ExamTypeName,
    DateTimeOffset TakenAt,
    AttemptStatus Status,
    int TotalCorrect,
    int TotalWrong,
    int TotalBlank,
    decimal TotalNet,
    decimal? ReportedScore,
    ScoreSource ScoreSource,
    DateTimeOffset? FinalizedAt,
    byte[] RowVersion,
    List<SubjectResultDetailDto> SubjectResults
);

public class GetResultDetailHandler(
    IApplicationDbContext db,
    IExamResourceAccess resourceAccess,
    IAcademicCatalog academicCatalog)
{
    public async Task<ExamResultDetailDto> HandleAsync(Guid studentId, Guid attemptId, CancellationToken ct = default)
    {
        await resourceAccess.ValidateStudentReadAccessAsync(studentId, ct);

        var attempt = await db.Set<StudentExamAttempt>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId, ct)
            ?? throw new NotFoundException("StudentExamAttempt", attemptId);

        var exam = await db.Set<Exam>()
            .AsNoTracking()
            .SingleAsync(e => e.Id == attempt.ExamId, ct);

        var sections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(s => s.ExamId == exam.Id)
            .ToDictionaryAsync(s => s.Id, ct);

        var subjectResults = await db.Set<StudentExamSubjectResult>()
            .AsNoTracking()
            .Where(r => r.StudentExamAttemptId == attemptId)
            .ToListAsync(ct);

        var topicResults = await db.Set<StudentExamTopicResult>()
            .AsNoTracking()
            .Where(r => r.StudentExamAttemptId == attemptId)
            .ToListAsync(ct);

        var examType = await academicCatalog.GetExamTypeAsync(exam.ExamTypeId, ct);

        var subjectIds = sections.Values.Select(s => s.SubjectId).Distinct();
        var topicIds = topicResults.Select(t => t.TopicId).Distinct();

        var subjectNames = await academicCatalog.GetSubjectNamesAsync(subjectIds, ct);
        var topicNames = await academicCatalog.GetTopicNamesAsync(topicIds, ct);

        var subjectDtos = new List<SubjectResultDetailDto>();

        foreach (var sr in subjectResults)
        {
            var sec = sections.GetValueOrDefault(sr.ExamSectionId);
            var subjectId = sec?.SubjectId ?? Guid.Empty;
            var subjectName = subjectNames.GetValueOrDefault(subjectId, "Unknown Subject");

            var sectionTopics = topicResults
                .Where(tr => tr.ExamSectionId == sr.ExamSectionId)
                .Select(tr => new TopicResultDetailDto(
                    tr.Id,
                    tr.TopicId,
                    topicNames.GetValueOrDefault(tr.TopicId, "Unknown Topic"),
                    tr.Correct,
                    tr.Wrong,
                    tr.Blank,
                    tr.Net
                )).ToList();

            subjectDtos.Add(new SubjectResultDetailDto(
                sr.Id,
                sr.ExamSectionId,
                subjectId,
                subjectName,
                sr.Correct,
                sr.Wrong,
                sr.Blank,
                sr.Net,
                sectionTopics
            ));
        }

        return new ExamResultDetailDto(
            attempt.Id,
            exam.Id,
            exam.Title,
            exam.Code,
            exam.ExamTypeId,
            examType?.Name ?? "Unknown",
            attempt.TakenAt,
            attempt.Status,
            attempt.TotalCorrect,
            attempt.TotalWrong,
            attempt.TotalBlank,
            attempt.TotalNet,
            attempt.ReportedScore,
            attempt.ScoreSource,
            attempt.FinalizedAt,
            attempt.RowVersion,
            subjectDtos
        );
    }
}
