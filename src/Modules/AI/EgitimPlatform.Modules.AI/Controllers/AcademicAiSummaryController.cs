using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.AI.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.AI.Controllers;

[ApiController]
[Route("api/v1/students/{studentId:guid}/academic-intelligence")]
[Authorize(Policy = Policies.CanViewExamResults)]
public class AcademicAiSummaryController(GetStudentAcademicAiSummaryHandler aiSummaryHandler) : ControllerBase
{
    [HttpGet("ai-summary")]
    public async Task<ActionResult<AcademicAiSummaryDto>> GetAiSummary(
        [FromRoute] Guid studentId,
        [FromQuery] string audience = "student",
        CancellationToken ct = default)
    {
        var result = await aiSummaryHandler.HandleAsync(studentId, audience, ct);
        return Ok(result);
    }
}
