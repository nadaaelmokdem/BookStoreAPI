using System.Net;
using System.Text.Json;
using BookStoreApi.Dtos.Common;
using BookStoreApi.Exceptions;

namespace BookStoreApi.Middleware;

/// <summary>
/// Catches every unhandled exception in the pipeline and converts it into a clean,
/// structured JSON response. Guarantees the API never leaks stack traces or internal
/// exception details to the client.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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
        var (statusCode, message, errors) = MapException(exception);

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Handled exception ({StatusCode}) on {Method} {Path}: {Message}",
                (int)statusCode, context.Request.Method, context.Request.Path, exception.Message);
        }

        var response = new ApiErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = message,
            TraceId = context.TraceIdentifier,
            Errors = errors
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private (HttpStatusCode StatusCode, string Message, IDictionary<string, string[]>? Errors) MapException(Exception ex) => ex switch
    {
        NotFoundException => (HttpStatusCode.NotFound, ex.Message, null),
        UnauthorizedException => (HttpStatusCode.Unauthorized, ex.Message, null),
        ForbiddenException => (HttpStatusCode.Forbidden, ex.Message, null),
        ConflictException => (HttpStatusCode.Conflict, ex.Message, null),
        BadRequestException => (HttpStatusCode.BadRequest, ex.Message, null),
        ValidationAppException vex => (HttpStatusCode.BadRequest, vex.Message, vex.Errors),
        _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.", null)
    };
}
