using WhistleblowerNews.Application.Auditing;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Web.Infrastructure;

public sealed class SecurityEventLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityEventLoggingMiddleware> _logger;

    public SecurityEventLoggingMiddleware(RequestDelegate next, ILogger<SecurityEventLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, IAuditService audit)
    {
        await _next(context);

        var statusCode = context.Response.StatusCode;
        if (statusCode != StatusCodes.Status403Forbidden && statusCode != StatusCodes.Status429TooManyRequests)
            return;

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.Equals("/health", StringComparison.OrdinalIgnoreCase))
            return;

        var target = $"{context.Request.Method} {path}";
        var auditContext = AuditContextFactory.FromHttpContext(context);

        if (statusCode == StatusCodes.Status403Forbidden)
        {
            _logger.LogWarning("Authorization denied for {Target}", target);
            await SafeAuditAsync(() => audit.Denied(
                AuditEventType.AuthorizationDenied,
                "Endpoint",
                target,
                null,
                auditContext));
        }

        if (statusCode == StatusCodes.Status429TooManyRequests)
        {
            _logger.LogWarning("Rate limit triggered for {Target}", target);
            await SafeAuditAsync(() => audit.Denied(
                AuditEventType.RateLimitTriggered,
                "Endpoint",
                target,
                null,
                auditContext));
        }
    }

    private static async Task SafeAuditAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch
        {
            // Do not break the response pipeline if audit logging fails.
        }
    }
}
