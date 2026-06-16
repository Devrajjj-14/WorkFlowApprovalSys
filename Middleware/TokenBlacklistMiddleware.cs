using WorkflowApprovalApi.Services.Implementations;

namespace WorkflowApprovalApi.Middleware;

// This middleware runs AFTER UseAuthentication.
// UseAuthentication already confirmed the token is valid (correct signature, not expired).
// This middleware adds one more check: is this token blacklisted (revoked after logout)?
public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenBlacklistMiddleware> _logger;

    public TokenBlacklistMiddleware(RequestDelegate next, ILogger<TokenBlacklistMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, TokenBlacklistService blacklistService)
    {
        // Read the Authorization header. Example value: "Bearer eyJhbGci..."
        var authHeader = context.Request.Headers["Authorization"].ToString();

        // Only check if a Bearer token is actually present
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            // Strip the "Bearer " prefix to get the raw token string
            var token = authHeader.Substring("Bearer ".Length).Trim();

            // Check if this token was blacklisted (i.e. user logged out)
            if (blacklistService.IsBlacklisted(token))
            {
                _logger.LogWarning(
                    "Blocked request from {Path} — token is blacklisted",
                    context.Request.Path);

                // Return 401 and stop the pipeline — controller never runs
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"message\": \"Token has been revoked. Please log in again.\"}");
                return;
            }
        }

        // Token is not blacklisted — continue to next middleware/controller
        await _next(context);
    }
}
