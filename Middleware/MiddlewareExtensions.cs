namespace WorkflowApprovalApi.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ServerTimingMiddleware>();
        app.UseMiddleware<TokenBlacklistMiddleware>();
        return app;
    }
}
