// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: Stopwatch doesn't exist — request duration can't be measured in milliseconds
using System.Diagnostics;

namespace WorkflowApprovalApi.Middleware;

// Logs every incoming HTTP request and its completion (status code + elapsed time)
// Registered second in UseCustomMiddleware() — runs inside ExceptionHandlingMiddleware's try block
// Without this middleware: no per-request audit trail in logs — you can't see what was called or how long it took
public class RequestLoggingMiddleware
{
    // Next middleware in the pipeline — after logging "Incoming", control passes here
    // Without this field: requests never proceed past this middleware
    private readonly RequestDelegate _next;

    // Serilog-backed logger — writes Information level entries to console and log file
    // Without this field: timing logic runs but nothing is written — logging middleware is silent
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    // DI injects the next pipeline step and the typed logger
    // Without this constructor: ASP.NET can't construct the middleware — startup fails
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // ── Pipeline Entry Point ────────────────────────────────────────────────
    // Captures method/path before the request, status/duration after controllers finish
    // Without this method: middleware is never executed — no request logs appear
    public async Task InvokeAsync(HttpContext context)
    {
        // High-resolution timer started at the beginning of the request
        // Without this: ElapsedMilliseconds below is always 0 — performance logging is useless
        var stopwatch = Stopwatch.StartNew();

        // HTTP verb — GET, POST, PUT, DELETE etc.
        // Without this: log lines show {Method} placeholder literally
        var method = context.Request.Method;

        // URL path — e.g. /api/projects/5
        // Without this: log lines show {Path} placeholder literally
        var path = context.Request.Path;

        // First log line when a request arrives — before controller runs
        // Without this: you only see completion logs — can't tell when a slow request started
        _logger.LogInformation("Incoming request {Method} {Path}", method, path);

        // Continues to CORS, authentication, authorization, and the matching controller action
        // Without this await: controller never runs — client hangs with no response
        await _next(context);

        // Stops the timer after the full pipeline (including controller) has finished
        // Without this: stopwatch keeps running — elapsed time would be wrong if read later
        stopwatch.Stop();

        // Second log line — includes HTTP status (200, 401, 404, 500) and total milliseconds
        // Used to spot slow endpoints and failed requests in logs/custom logger YYYY-MM-DD.log
        // Without this: you know a request came in but not whether it succeeded or how long it took
        _logger.LogInformation(
            "Completed request {Method} {Path} with status {StatusCode} in {ElapsedMs}ms",
            method,
            path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}
