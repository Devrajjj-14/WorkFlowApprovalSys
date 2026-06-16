// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: Stopwatch doesn't exist — we can't measure how long each request takes
using System.Diagnostics;

// Without this: ServerTimingMiddleware lives in the global namespace — breaks project structure
namespace WorkflowApprovalApi.Middleware;

// ServerTimingMiddleware measures how long each API request takes and adds it to the response header
// The frontend reads X-Response-Time after login to show "Login processed in Xms by the server"
// Without this class: no response timing header — login page can't display server processing time
public class ServerTimingMiddleware
{
    // Holds the next middleware in the pipeline — we call this after measuring time
    // Without this field: we can't pass the request to the rest of the pipeline
    private readonly RequestDelegate _next;

    // Constructor — ASP.NET injects the next middleware delegate automatically
    // Without this constructor: DI can't create this middleware — pipeline registration fails
    public ServerTimingMiddleware(RequestDelegate next)
    {
        // Stores the next step so InvokeAsync can forward the request after setup
        // Without this assignment: _next is null — NullReferenceException on every request
        _next = next;
    }

    // Runs on every HTTP request — starts a timer, forwards the request, then adds timing header
    // Without this method: middleware does nothing — X-Response-Time header is never sent
    public async Task InvokeAsync(HttpContext context)
    {
        // Starts a high-resolution stopwatch to measure elapsed milliseconds for this request
        // Without this: we have no way to know how long the request took
        var stopwatch = Stopwatch.StartNew();

        // Register a callback that fires just BEFORE headers are sent to the client
        // This is needed because once the response starts writing, headers are locked and can't be changed
        // OnStarting guarantees we can still add the X-Response-Time header at the last safe moment
        // Without this callback: header would be added too late or not at all — frontend gets no timing
        context.Response.OnStarting(() =>
        {
            // Stops the timer now that the response is about to go out
            // Without this: elapsed time keeps growing and the header shows wrong values
            stopwatch.Stop();

            // Writes the elapsed time into the X-Response-Time response header (e.g. "42ms")
            // ApiService.LoginAsync reads this header and passes it to AuthController for TempData display
            // Without this line: the header is never set — login page can't show server timing
            context.Response.Headers["X-Response-Time"] = $"{stopwatch.ElapsedMilliseconds}ms";

            // OnStarting expects a Task return — CompletedTask means "callback finished, no async work"
            // Without this return: the callback signature doesn't match — compile error
            return Task.CompletedTask;
        });

        // Passes the request to the next middleware (controllers, auth, etc.) and waits for it to finish
        // Without this: the request never reaches your API endpoints — every call hangs or fails
        await _next(context);
    }
}
