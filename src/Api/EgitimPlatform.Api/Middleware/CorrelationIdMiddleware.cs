using Serilog.Context;
using System.Text.RegularExpressions;

namespace EgitimPlatform.Api.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const int MaxLength = 100;

    // Accept GUID, ULID, or alphanumeric with hyphens/underscores
    private static readonly Regex ValidFormat = new(
        @"^[A-Za-z0-9\-_]{1,100}$",
        RegexOptions.Compiled);

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId;

        var clientValue = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(clientValue) && IsValidCorrelationId(clientValue))
        {
            correlationId = clientValue;
        }
        else
        {
            // Invalid or missing → generate server-side
            correlationId = Guid.CreateVersion7().ToString();
        }

        context.Request.Headers[CorrelationIdHeader] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static bool IsValidCorrelationId(string value)
    {
        if (value.Length > MaxLength) return false;
        return ValidFormat.IsMatch(value);
    }
}
