using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Identity.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Identity.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = Policies.CanManageInstitution)]
public class UsersController(UserHandler handler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserCommand command, CancellationToken ct)
    {
        var result = await handler.CreateAsync(command, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List([FromQuery] ListUsersQuery query, CancellationToken ct)
    {
        var result = await handler.ListAsync(query, ct);
        return Ok(result);
    }
}
