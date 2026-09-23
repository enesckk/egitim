using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.GetExamDetail;

public record ExamSectionDetailDto(
    Guid Id,
    Guid SubjectId,
    string SubjectName,
    int DisplayOrder,
    int QuestionCount,
    decimal EffectiveWrongAnswerPenalty
);

public record ExamDetailDto(
    Guid Id,
    Guid InstitutionId,
    Guid ExamTypeId,
    string ExamTypeName,
    string Code,
    string Title,
    DateTimeOffset ExamDate,
    ExamStatus Status,
    decimal WrongAnswerPenalty,
    ScoreSource ScoreSource,
    List<ExamSectionDetailDto> Sections
);

public class GetExamDetailHandler(
    IApplicationDbContext db,
    IExamResourceAccess resourceAccess,
    IAcademicCatalog academicCatalog)
{
    public async Task<ExamDetailDto> HandleAsync(Guid examId, CancellationToken ct = default)
    {
        var exam = await resourceAccess.ValidateExamAccessAsync(examId, ct);

        var sections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(s => s.ExamId == examId)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

        var examType = await academicCatalog.GetExamTypeAsync(exam.ExamTypeId, ct);
        var subjectIds = sections.Select(s => s.SubjectId);
        var subjectNames = await academicCatalog.GetSubjectNamesAsync(subjectIds, ct);

        var sectionDtos = sections.Select(s => new ExamSectionDetailDto(
            s.Id,
            s.SubjectId,
            subjectNames.GetValueOrDefault(s.SubjectId, "Unknown Subject"),
            s.DisplayOrder,
            s.QuestionCount,
            s.WrongAnswerPenalty ?? exam.WrongAnswerPenalty
        )).ToList();

        return new ExamDetailDto(
            exam.Id,
            exam.InstitutionId,
            exam.ExamTypeId,
            examType?.Name ?? "Unknown ExamType",
            exam.Code,
            exam.Title,
            exam.ExamDate,
            exam.Status,
            exam.WrongAnswerPenalty,
            exam.ScoreSource,
            sectionDtos
        );
    }
}
