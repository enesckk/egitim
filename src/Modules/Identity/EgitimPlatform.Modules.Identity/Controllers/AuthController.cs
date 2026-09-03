using Microsoft.AspNetCore.Hosting;
using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Identity.Features.Logout;
using EgitimPlatform.Modules.Identity.Features.Refresh;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace EgitimPlatform.Modules.Identity.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "rt";
    private readonly LoginHandler _loginHandler;
    private readonly RefreshHandler _refreshHandler;
    private readonly LogoutHandler _logoutHandler;
    private readonly IValidator<LoginCommand> _loginValidator;
    private readonly JwtSettings _jwtSettings;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        LoginHandler loginHandler,
        RefreshHandler refreshHandler,
        LogoutHandler logoutHandler,
        IValidator<LoginCommand> loginValidator,
        IOptions<JwtSettings> jwtSettings,
        IWebHostEnvironment environment)
    {
        _loginHandler = loginHandler;
        _refreshHandler = refreshHandler;
        _logoutHandler = logoutHandler;
        _loginValidator = loginValidator;
        _jwtSettings = jwtSettings.Value;
        _environment = environment;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var validationResult = await _loginValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(
                title: "Validation failed",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                modelStateDictionary: CreateModelState(validationResult));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _loginHandler.HandleAsync(command, ipAddress, ct);

        if (result.Status != LoginHandler.LoginStatus.Success
            || result.Tokens is null
            || result.RawRefreshToken is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed",
                Detail = "Invalid credentials.",
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Instance = HttpContext.Request.Path.Value,
            });
        }

        // P2-3: Refresh token goes into HttpOnly Secure cookie ONLY (not JSON body).
        SetRefreshTokenCookie(result.RawRefreshToken);

        // P2-3: Response DTO no longer contains RefreshToken field.
        return Ok(new LoginResponseDto(
            result.Tokens.AccessToken,
            result.Tokens.AccessTokenExpiresAt));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        // P2-3: Read refresh token from cookie only
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var rawToken) ||
            string.IsNullOrWhiteSpace(rawToken))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed",
                Detail = "Refresh token not found.",
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Instance = HttpContext.Request.Path.Value,
            });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new RefreshCommand(rawToken);
        var result = await _refreshHandler.HandleAsync(command, ipAddress, ct);

        if (result is null)
        {
            ClearRefreshTokenCookie();
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed",
                Detail = "Invalid refresh token.",
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Instance = HttpContext.Request.Path.Value,
            });
        }

        // Set new refresh token cookie (rotation)
        SetRefreshTokenCookie(result.NewRawRefreshToken);

        // P2-3: No refresh token in JSON body
        return Ok(new LoginResponseDto(
            result.AccessToken,
            result.AccessTokenExpiresAt));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        Request.Cookies.TryGetValue(RefreshTokenCookieName, out var rawToken);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new LogoutCommand(rawToken);
        await _logoutHandler.HandleAsync(command, ipAddress, ct);

        ClearRefreshTokenCookie();
        return NoContent();
    }

    /// <summary>
    /// P2-3/P2-4: Cookie attributes hardened.
    /// Secure flag: always true in Production/Staging; in Development follows Request.IsHttps.
    /// Behind a reverse proxy, ForwardedHeaders middleware must be configured so
    /// Request.IsHttps reflects the original client scheme (see Program.cs).
    /// </summary>
    private void SetRefreshTokenCookie(string refreshToken)
    {
        // P2-4: In Production, Secure is always true regardless of Request.IsHttps
        // (the proxy terminates TLS and forwards via ForwardedHeaders).
        var isSecure = _environment.EnvironmentName == "Production"
                       || _environment.EnvironmentName == "Staging"
                       || HttpContext.Request.IsHttps;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            MaxAge = TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays),
        };
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            Path = "/api/v1/auth",
        });
    }

    private static ModelStateDictionary CreateModelState(FluentValidation.Results.ValidationResult validationResult)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in validationResult.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
        return modelState;
    }
}
