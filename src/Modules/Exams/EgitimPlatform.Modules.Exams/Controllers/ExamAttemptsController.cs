using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Exams.Features.CreateAttempt;
using EgitimPlatform.Modules.Exams.Features.FinalizeAttempt;
using EgitimPlatform.Modules.Exams.Features.GetResultDetail;
using EgitimPlatform.Modules.Exams.Features.GetStudentAnalysis;
using EgitimPlatform.Modules.Exams.Features.ListExams;
using EgitimPlatform.Modules.Exams.Features.ListStudentResults;
using EgitimPlatform.Modules.Exams.Features.UpdateAttempt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Exams.Controllers;

[ApiController]
public class ExamAttemptsController : ControllerBase
{
    [HttpPost("api/v1/exams/{examId:guid}/attempts")]
    [Authorize(Policy = Policies.CanRecordExamResults)]
    public async Task<ActionResult<Guid>> CreateAttempt(
        [FromRoute] Guid examId,
        [FromBody] CreateAttemptCommand command,
        [FromServices] CreateAttemptHandler handler,
        CancellationToken ct)
    {
        var attemptId = await handler.HandleAsync(examId, command, ct);
        return CreatedAtAction(nameof(GetResultDetail), new { studentId = command.StudentId, attemptId }, attemptId);
    }

    [HttpPut("api/v1/exam-attempts/{attemptId:guid}")]
    [Authorize(Policy = Policies.CanRecordExamResults)]
    public async Task<IActionResult> UpdateAttempt(
        [FromRoute] Guid attemptId,
        [FromBody] UpdateAttemptCommand command,
        [FromServices] UpdateAttemptHandler handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(attemptId, command, ct);
        return NoContent();
    }

    [HttpPost("api/v1/exam-attempts/{attemptId:guid}/finalize")]
    [Authorize(Policy = Policies.CanRecordExamResults)]
    public async Task<IActionResult> FinalizeAttempt(
        [FromRoute] Guid attemptId,
        [FromServices] FinalizeAttemptHandler handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(attemptId, ct);
        return NoContent();
    }

    [HttpGet("api/v1/students/{studentId:guid}/exam-results")]
    [Authorize(Policy = Policies.CanViewExamResults)]
    public async Task<ActionResult<PaginatedList<StudentExamResultSummaryDto>>> ListStudentResults(
        [FromRoute] Guid studentId,
        [FromQuery] ListStudentResultsQuery query,
        [FromServices] ListStudentResultsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(studentId, query, ct);
        return Ok(result);
    }

    [HttpGet("api/v1/students/{studentId:guid}/exam-results/{attemptId:guid}")]
    [Authorize(Policy = Policies.CanViewExamResults)]
    public async Task<ActionResult<ExamResultDetailDto>> GetResultDetail(
        [FromRoute] Guid studentId,
        [FromRoute] Guid attemptId,
        [FromServices] GetResultDetailHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(studentId, attemptId, ct);
        return Ok(result);
    }

    [HttpGet("api/v1/students/{studentId:guid}/exam-analysis")]
    [Authorize(Policy = Policies.CanViewExamResults)]
    public async Task<ActionResult<StudentExamAnalysisDto>> GetStudentAnalysis(
        [FromRoute] Guid studentId,
        [FromQuery] GetStudentAnalysisQuery query,
        [FromServices] GetStudentAnalysisHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(studentId, query, ct);
        return Ok(result);
    }
}
