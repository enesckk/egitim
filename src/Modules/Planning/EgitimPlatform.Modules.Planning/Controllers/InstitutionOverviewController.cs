using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Planning.Features.GetInstitutionOverview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Planning.Controllers;

[ApiController]
[Route("api/v1/institution/overview")]
[Authorize(Policy = Policies.CanManageInstitution)]
public class InstitutionOverviewController(GetInstitutionOverviewHandler handler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(InstitutionOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InstitutionOverviewDto>> GetOverview(CancellationToken ct)
    {
        var result = await handler.HandleAsync(ct);
        return Ok(result);
    }
}
