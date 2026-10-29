using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EgitimPlatform.Api.Health;

public static class HealthCheckEndpointExtensions
{
    private static readonly byte[] HealthyBytes = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"Healthy\"}");
    private static readonly byte[] DegradedBytes = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"Degraded\"}");
    private static readonly byte[] UnhealthyBytes = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"Unhealthy\"}");

    public static Task WriteSanitizedHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var bytes = report.Status switch
        {
            HealthStatus.Healthy => HealthyBytes,
            HealthStatus.Degraded => DegradedBytes,
            _ => UnhealthyBytes
        };

        return context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
    }
}
