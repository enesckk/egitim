using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Students.Features.ManageParents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EgitimPlatform.Modules.Students.Controllers;
[ApiController, Route("api/v1/parents"), Authorize(Roles = Roles.SuperAdmin + "," + Roles.InstitutionAdmin)]
public class ParentsController(ParentHandler handler) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ParentDto>> Create(CreateParentCommand c, CancellationToken ct) => StatusCode(201, await handler.CreateAsync(c, ct));
    [HttpPost("relationships")]
    public async Task<ActionResult<StudentParentDto>> Link(LinkParentCommand c, CancellationToken ct) => StatusCode(201, await handler.LinkAsync(c, ct));
    [HttpPut("relationships/{id:guid}")]
    public async Task<ActionResult<StudentParentDto>> SetActive(Guid id, SetParentRelationshipCommand c, CancellationToken ct)
    {
        if (id != c.RelationshipId) return ValidationProblem("Path/body mismatch.");
        return Ok(await handler.SetActiveAsync(c, ct));
    }
}
