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

        // P2-10: Reject multiple X-Correlation-Id values — ambiguous input.
        // If the header has more than one value, treat it as invalid and generate server-side.
        var headerValues = context.Request.Headers[CorrelationIdHeader];
        var headerCount = headerValues.Count;

        if (headerCount == 1)
        {
            var clientValue = headerValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(clientValue) && IsValidCorrelationId(clientValue))
            {
                correlationId = clientValue;
            }
            else
            {
                correlationId = Guid.CreateVersion7().ToString();
            }
        }
        else
        {
            // 0 values (missing) or >1 values (ambiguous) → server-generated
            correlationId = Guid.CreateVersion7().ToString();
        }

        // Replace all values with exactly one canonical value
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
