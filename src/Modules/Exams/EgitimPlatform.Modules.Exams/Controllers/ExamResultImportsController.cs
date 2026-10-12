using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Exams.Features.ExamResultImport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Exams.Controllers;

[ApiController]
[Route("api/v1/exams/{examId:guid}/result-imports")]
public class ExamResultImportsController : ControllerBase
{
    [HttpGet("templates/aggregate")]
    [Authorize(Policy = Policies.CanRecordExamResults)]
    public async Task<IActionResult> GetAggregateTemplate(
        [FromRoute] Guid examId,
        [FromServices] GenerateImportTemplateHandler handler,
        CancellationToken ct)
    {
        var (bytes, contentType, fileName) = await handler.HandleAggregateTemplateAsync(examId, ct);
        return File(bytes, contentType, fileName);
    }

    [HttpGet("templates/question-answers")]
    [Authorize(Policy = Policies.CanRecordExamResults)]
    public async Task<IActionResult> GetQuestionAnswersTemplate(
        [FromRoute] Guid examId,
        [FromServices] GenerateImportTemplateHandler handler,
        CancellationToken ct)
    {
        var (bytes, contentType, fileName) = await handler.HandleQuestionAnswersTemplateAsync(examId, ct);
        return File(bytes, contentType, fileName);
    }

    [HttpPost("preview")]
    [Authorize(Policy = Policies.CanRecordExamResults)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ExamResultImportPreviewDto>> PreviewImport(
        [FromRoute] Guid examId,
        [FromForm] ImportType importType,
        [FromForm] IFormFile file,
        [FromServices] PreviewResultImportHandler handler,
        CancellationToken ct)
    {
        var preview = await handler.HandleAsync(examId, importType, file, ct);
        return Ok(preview);
    }

    [HttpPost("commit")]
    [Authorize(Policy = Policies.CanRecordExamResults)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ExamResultImportCommitResultDto>> CommitImport(
        [FromRoute] Guid examId,
        [FromForm] ImportType importType,
        [FromForm] string fileHash,
        [FromForm] IFormFile file,
        [FromServices] CommitResultImportHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(examId, importType, fileHash, file, ct);
        return Ok(result);
    }
}
