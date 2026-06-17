using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace API.Middleware;

/// <summary>
/// The outermost piece of the request pipeline: it wraps everything downstream in a
/// try/catch so that any exception thrown by a controller or service is turned into a
/// single, consistent JSON error response (RFC 7807 ProblemDetails) with the correct
/// HTTP status code. This is why the service layer can simply THROW domain exceptions
/// (NotFoundException, ValidationException) instead of returning error codes — the
/// translation to HTTP lives here, in the API layer, exactly once.
/// </summary>
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
            // Pass the request to the next component in the pipeline.
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Map each known exception type to its HTTP status + a short, safe title.
        // Anything unmapped is a genuine 500 we did not anticipate.
        var (statusCode, title) = exception switch
        {
            NotFoundException   => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            _                   => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        // Expected domain exceptions are warnings (client's fault); a 500 is a real
        // server-side bug worth an error-level log with the full stack trace.
        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception processing {Path}", context.Request.Path);
        else
            _logger.LogWarning("Handled {ExceptionType}: {Message}", exception.GetType().Name, exception.Message);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            // Never leak internal exception text on a 500 — could expose stack/SQL details.
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred. Please try again later."
                : exception.Message,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = statusCode;
        // Pass the content type through the JSON writer; otherwise WriteAsJsonAsync
        // resets it to "application/json" and the problem+json signal is lost.
        await context.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json");
    }
}
