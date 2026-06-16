namespace WorkflowApprovalApi.Middleware;

// Extension methods that register custom middleware on the ASP.NET Core pipeline
// Called from Program.cs as app.UseCustomMiddleware() — keeps Program.cs readable
// Without this class: Program.cs must call UseMiddleware<> twice manually — same behavior, messier setup
public static class MiddlewareExtensions
{
    // ── Register Custom Middleware in Order ─────────────────────────────────
    // Adds ExceptionHandlingMiddleware first, then RequestLoggingMiddleware, then ServerTimingMiddleware
    // Order matters: ExceptionHandling wraps RequestLogging so logging runs inside the try/catch
    // Without this method: UseCustomMiddleware() in Program.cs doesn't compile — middleware never added
    public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder app)
    {
        // Outermost wrapper — catches exceptions from everything below including RequestLogging and controllers
        // Without this line: unhandled exceptions aren't converted to JSON — clients get default error pages
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Inner layer — logs incoming/completed requests; runs after exception handler is registered but inside its try block
        // Without this line: no request logging — Serilog file never shows per-request lines
        app.UseMiddleware<RequestLoggingMiddleware>();

        // Adds X-Response-Time header to every response — frontend reads this after login to show server timing
        // Without this line: AuthController can't display "Login processed in Xms" message to the user
        app.UseMiddleware<ServerTimingMiddleware>();

        // Returns app so Program.cs can chain further calls (UseCors, UseAuthentication, etc.)
        // Without this return: extension method can't be used in a fluent chain — compile error
        return app;
    }
}
