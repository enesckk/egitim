using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Reporting.Features.InstitutionalReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Reporting.Controllers;

[ApiController]
[Route("api/v1/reporting/institution")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.InstitutionAdmin}")]
public class InstitutionalReportsController : ControllerBase
{
    private readonly IInstitutionalReportHandler _handler;

    public InstitutionalReportsController(IInstitutionalReportHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(InstitutionalReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReport(
        [FromQuery] Guid? classroomId,
        [FromQuery] Guid? examId,
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        CancellationToken ct)
    {
        var filter = new ReportFilterRequest(classroomId, examId, startDate, endDate);
        var result = await _handler.GetReportDataAsync(filter, ct);
        return Ok(result);
    }

    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportReport(
        [FromQuery] string? format,
        [FromQuery] Guid? classroomId,
        [FromQuery] Guid? examId,
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        CancellationToken ct)
    {
        var filter = new ReportFilterRequest(classroomId, examId, startDate, endDate);
        var requestedFormat = format ?? "xlsx";
        var (content, contentType, fileName) = await _handler.ExportReportAsync(filter, requestedFormat, ct);
        return File(content, contentType, fileName);
    }
}
