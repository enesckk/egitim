using EgitimPlatform.Modules.Exams.Services;
using Microsoft.AspNetCore.Http;

namespace EgitimPlatform.Modules.Exams.Features.ExamResultImport;

public class PreviewResultImportHandler(
    IExamResourceAccess resourceAccess,
    IExamResultImportPreviewEngine previewEngine)
{
    public async Task<ExamResultImportPreviewDto> HandleAsync(
        Guid examId,
        ImportType importType,
        IFormFile file,
        CancellationToken ct = default)
    {
        var exam = await resourceAccess.ValidateExamAccessAsync(examId, ct);
        return await previewEngine.GeneratePreviewAsync(examId, exam.InstitutionId, importType, file, ct);
    }
}
