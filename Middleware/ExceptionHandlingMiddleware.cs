// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: HttpStatusCode enum doesn't exist — status codes can't be mapped from exception types
using System.Net;

// Without this: JsonSerializer doesn't exist — error responses can't be serialized to JSON
using System.Text.Json;

namespace WorkflowApprovalApi.Middleware;

// Catches any unhandled exception thrown anywhere in the request pipeline
// Registered first in UseCustomMiddleware() so it wraps all later middleware and controllers
// Without this middleware: unhandled exceptions return HTML error pages or empty 500 responses
public class ExceptionHandlingMiddleware
{
    // Reference to the next middleware in the pipeline — InvokeAsync calls this to continue the request
    // Without this field: the pipeline stops here — no controller ever runs
    private readonly RequestDelegate _next;

    // Structured logger — writes errors to console, file, and Application Insights via Serilog
    // Without this field: exceptions are caught but never logged — debugging production failures is impossible
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    // Tells us if we're in Development or Production — controls whether real exception text is sent to clients
    // Without this field: production might leak stack traces OR development might hide useful error messages
    private readonly IHostEnvironment _environment;

    // Constructor — DI injects the next delegate, logger, and environment automatically
    // Without this constructor: middleware can't be activated — app fails at startup when pipeline builds
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    // ── Pipeline Entry Point ────────────────────────────────────────────────
    // Every HTTP request passes through here before and after the rest of the pipeline
    // Without this method: middleware isn't invoked — exception handling never runs
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Passes control to the next middleware (RequestLogging → CORS → auth → controllers)
            // Without this await: the request never reaches endpoints — everything hangs or returns empty
            await _next(context);
        }
        catch (Exception ex)
        {
            // Logs method, path, and full exception — appears in logs/custom logger YYYY-MM-DD.log
            // Without this: client gets JSON error but server logs stay silent — no trace of the failure
            _logger.LogError(
                ex,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            // Converts the exception into a JSON response with appropriate HTTP status code
            // Without this: exception is swallowed after logging — client may get a broken connection
            await HandleExceptionAsync(context, ex);
        }
    }

    // ── Map Exception Type → HTTP Status + Message ──────────────────────────
    // Services throw familiar .NET exceptions; this middleware translates them to REST-friendly responses
    // Without this method: InvokeAsync catch block has no way to build the response body
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Pattern match — first matching type wins; default is 500 Internal Server Error
        // UnauthorizedAccessException → 401 (e.g. wrong password or forbidden action)
        // InvalidOperationException / ArgumentException → 400 (business rule or validation failure)
        // FileNotFoundException / KeyNotFoundException → 404 (missing entity)
        // _ => 500 with generic message in Production, real message in Development
        // Without this switch: every error would be 500 with the same vague message
        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            FileNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            _ => (HttpStatusCode.InternalServerError,
                _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred.")
        };

        // Tells the client the body is JSON — frontend ParseAsync expects application/json
        // Without this: browsers may misinterpret the response content type
        context.Response.ContentType = "application/json";

        // Sets HTTP status (401, 400, 404, 500) on the response before writing the body
        // Without this: client always sees 200 OK with an error JSON — wrong semantics
        context.Response.StatusCode = (int)statusCode;

        // Shape matches what ApiService.ParseAsync reads — { "message": "..." }
        // Without this serialization step: response body is empty — frontend error parsing fails
        var payload = JsonSerializer.Serialize(new { message });

        // Writes the JSON string to the response stream and sends it to the client
        // Without this: status code is set but body is empty — user sees blank error
        await context.Response.WriteAsync(payload);
    }
}
