using System.Diagnostics;

namespace WorkflowApprovalApi.Middleware;

public class ServerTimingMiddleware
{
    private readonly RequestDelegate _next;

    public ServerTimingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // Register a callback that fires just BEFORE headers are sent to the client.
        // This is needed because once the response starts writing, headers are locked.
        // OnStarting guarantees we can still add headers at this point.
        context.Response.OnStarting(() =>
        {
            stopwatch.Stop();
            context.Response.Headers["X-Response-Time"] = $"{stopwatch.ElapsedMilliseconds}ms";
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
