using EgitimPlatform.Modules.Academic.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EgitimPlatform.Modules.Academic.Controllers;
[ApiController, Authorize, Route("api/v1/academic")]
public class TaxonomyController(TaxonomyHandler handler) : ControllerBase
{
    [HttpGet("{level}")]
    public async Task<ActionResult<IReadOnlyList<TaxonomyDto>>> Get(string level, [FromQuery] Guid? parentId, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 100) =>
        Ok(await handler.HandleAsync(new(level, parentId, page, pageSize), ct));
}
