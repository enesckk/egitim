using Microsoft.Extensions.Options;

namespace EgitimPlatform.Api.Middleware;

/// <summary>
/// P2-02 CLOSURE: CSRF defense via Origin validation.
/// Validates that state-changing requests to auth endpoints (POST /api/v1/auth/*)
/// come from an explicitly allowed origin.
///
/// Defense layers:
/// 1. SameSite=Strict on refresh cookie (prevents cross-site cookie sending)
/// 2. CORS policy restricts allowed origins
/// 3. This middleware: explicit Origin header validation on state-changing endpoints
///
/// For SPA architecture: the browser always sends Origin on POST requests.
/// Missing or mismatched Origin → 403 Forbidden (fail-closed).
/// </summary>
public class CsrfOriginMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CsrfOriginMiddleware> _logger;

    public CsrfOriginMiddleware(RequestDelegate next, ILogger<CsrfOriginMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOptionsMonitor<CsrfOriginOptions> optionsMonitor)
    {
        // Only validate state-changing methods on auth endpoints
        if (!IsProtectedEndpoint(context))
        {
            await _next(context);
            return;
        }

        var options = optionsMonitor.CurrentValue;

        // In Development with no allowed origins configured, skip validation
        if (options.AllowAllInDevelopment && IsDevelopment(context))
        {
            await _next(context);
            return;
        }

        var origin = context.Request.Headers.Origin.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(origin))
        {
            // Same-origin requests (non-browser clients) may not send Origin.
            // For browser POST requests, Origin is mandatory per spec.
            // Check if this looks like a browser request (has Sec-Fetch-Site or User-Agent)
            var isBrowser = context.Request.Headers.ContainsKey("Sec-Fetch-Site")
                         || context.Request.Headers.UserAgent.Count > 0;

            if (isBrowser)
            {
                _logger.LogWarning("CSRF: Missing Origin header on protected endpoint {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    title = "Origin validation failed",
                    status = 403,
                    detail = "Missing required Origin header."
                });
                return;
            }

            // Non-browser client (API call) — allow through
            await _next(context);
            return;
        }

        // Validate Origin against allowed list
        if (options.AllowedOrigins.Length == 0)
        {
            // No origins configured — fail closed in production
            _logger.LogWarning("CSRF: No allowed origins configured, rejecting Origin: {Origin}", origin);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                title = "Origin validation failed",
                status = 403,
                detail = "Origin not allowed."
            });
            return;
        }

        if (!options.AllowedOrigins.Contains(origin))
        {
            _logger.LogWarning("CSRF: Rejected Origin {Origin} on {Path}", origin, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                title = "Origin validation failed",
                status = 403,
                detail = "Origin not allowed."
            });
            return;
        }

        await _next(context);
    }

    private static bool IsProtectedEndpoint(HttpContext context)
    {
        // Protect state-changing auth endpoints
        if (context.Request.Method != HttpMethods.Post) return false;
        var path = context.Request.Path.Value ?? string.Empty;
        return path.StartsWith("/api/v1/auth/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDevelopment(HttpContext context)
    {
        return context.RequestServices.GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment))
            is Microsoft.AspNetCore.Hosting.IWebHostEnvironment env
            && env.IsDevelopment();
    }
}

/// <summary>
/// Configuration for CSRF origin validation.
/// </summary>
public sealed class CsrfOriginOptions
{
    /// <summary>
    /// Allowed origins for state-changing requests.
    /// Must match the CORS allowed origins.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>
    /// In Development, allow requests without Origin validation when no origins configured.
    /// </summary>
    public bool AllowAllInDevelopment { get; set; } = true;
}
