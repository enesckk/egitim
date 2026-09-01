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

    public AuthController(
        LoginHandler loginHandler,
        RefreshHandler refreshHandler,
        LogoutHandler logoutHandler,
        IValidator<LoginCommand> loginValidator,
        IOptions<JwtSettings> jwtSettings)
    {
        _loginHandler = loginHandler;
        _refreshHandler = refreshHandler;
        _logoutHandler = logoutHandler;
        _loginValidator = loginValidator;
        _jwtSettings = jwtSettings.Value;
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

        if (result.Status != LoginHandler.LoginStatus.Success)
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

        // P2-08: Refresh token goes into HttpOnly Secure cookie (not JSON body).
        // Access token stays in JSON response (frontend holds in memory).
        SetRefreshTokenCookie(result.Tokens!.RefreshToken);

        // Return response WITHOUT refresh token in body
        return Ok(new LoginResponseDto(
            result.Tokens.AccessToken,
            RefreshToken: string.Empty, // Client reads refresh from cookie
            result.Tokens.AccessTokenExpiresAt));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        // P2-08: Read refresh token from cookie
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
            // Clear the cookie on any failure to prevent reuse
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
        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(new LoginResponseDto(
            result.AccessToken,
            RefreshToken: string.Empty, // Client reads from cookie
            result.AccessTokenExpiresAt));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        // P2-08: Read refresh token from cookie for revocation
        Request.Cookies.TryGetValue(RefreshTokenCookieName, out var rawToken);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new LogoutCommand(rawToken);
        await _logoutHandler.HandleAsync(command, ipAddress, ct);

        ClearRefreshTokenCookie();
        return NoContent();
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var isSecure = HttpContext.Request.IsHttps;
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure, // Secure in production (HTTPS), relaxed in dev/test
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth", // Only sent to auth endpoints
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
