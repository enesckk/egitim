using EgitimPlatform.Modules.Exams.Services;

namespace EgitimPlatform.Modules.Exams.Features.ExamResultImport;

public class GenerateImportTemplateHandler(
    IExamResourceAccess resourceAccess,
    IExamResultTemplateGenerator templateGenerator)
{
    public async Task<(byte[] FileBytes, string ContentType, string FileName)> HandleAggregateTemplateAsync(Guid examId, CancellationToken ct = default)
    {
        var exam = await resourceAccess.ValidateExamAccessAsync(examId, ct);
        var bytes = await templateGenerator.GenerateAggregateTemplateAsync(examId, exam.InstitutionId, ct);

        var fileName = $"Exam_{exam.Code}_Aggregate_Template.xlsx";
        return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    public async Task<(byte[] FileBytes, string ContentType, string FileName)> HandleQuestionAnswersTemplateAsync(Guid examId, CancellationToken ct = default)
    {
        var exam = await resourceAccess.ValidateExamAccessAsync(examId, ct);
        var bytes = await templateGenerator.GenerateQuestionAnswersTemplateAsync(examId, exam.InstitutionId, ct);

        var fileName = $"Exam_{exam.Code}_Questions_Template.xlsx";
        return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
