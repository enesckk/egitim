using EgitimPlatform.BuildingBlocks.Exceptions;
using System.Text.Json;

namespace EgitimPlatform.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // P2-05: Handle DbUpdateException (unique constraint violations) globally.
        // If a handler didn't catch it specifically, we still return 409 not 500.
        if (exception is Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database update error: {Message}", dbEx.Message);

            if (IsUniqueConstraintViolation(dbEx))
            {
                await WriteProblemResponse(context, StatusCodes.Status409Conflict, "Conflict",
                    "A conflicting record already exists. Please check and retry.");
                return;
            }

            // Other DB update errors → 500 with sanitized message
            await WriteProblemResponse(context, StatusCodes.Status500InternalServerError,
                "Database error", "An unexpected database error occurred.");
            return;
        }

        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Access denied"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        _logger.LogError(exception, "Request error: {Title} — {Message}", title, exception.Message);

        await WriteProblemResponse(context, statusCode, title,
            statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message);
    }

    private async Task WriteProblemResponse(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problem = new
        {
            type = GetProblemType(statusCode),
            title,
            status = statusCode,
            detail,
            instance = context.Request.Path.Value,
        };

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Detects SQL Server unique constraint violation (error 2601 or 2627).
    /// </summary>
    private static bool IsUniqueConstraintViolation(Microsoft.EntityFrameworkCore.DbUpdateException ex)
    {
        var inner = ex.InnerException;
        if (inner is null) return false;
        var typeName = inner.GetType().FullName ?? string.Empty;
        if (!typeName.Contains("SqlException")) return false;
        var numberProperty = inner.GetType().GetProperty("Number");
        if (numberProperty?.GetValue(inner) is int number)
            return number is 2601 or 2627;
        return false;
    }

    private static string GetProblemType(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
        403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
    };
}
