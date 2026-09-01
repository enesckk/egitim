using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Identity.Features.Logout;
using EgitimPlatform.Modules.Identity.Features.Refresh;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Identity.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly LoginHandler _loginHandler;
    private readonly RefreshHandler _refreshHandler;
    private readonly LogoutHandler _logoutHandler;
    private readonly IValidator<LoginCommand> _loginValidator;

    public AuthController(
        LoginHandler loginHandler,
        RefreshHandler refreshHandler,
        LogoutHandler logoutHandler,
        IValidator<LoginCommand> loginValidator)
    {
        _loginHandler = loginHandler;
        _refreshHandler = refreshHandler;
        _logoutHandler = logoutHandler;
        _loginValidator = loginValidator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var validationResult = await _loginValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.ToDictionary());

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _loginHandler.HandleAsync(command, ipAddress, ct);

        if (result is null)
            return Unauthorized(new { message = "Invalid credentials." });

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
            return BadRequest(new { message = "Refresh token is required." });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _refreshHandler.HandleAsync(command, ipAddress, ct);

        if (result is null)
            return Unauthorized(new { message = "Invalid refresh token." });

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _logoutHandler.HandleAsync(command, ipAddress, ct);
        return NoContent();
    }
}
