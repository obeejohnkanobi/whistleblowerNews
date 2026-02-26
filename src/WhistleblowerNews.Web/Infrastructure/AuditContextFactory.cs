using System.Security.Claims;
using WhistleblowerNews.Application.Auditing;

namespace WhistleblowerNews.Web.Infrastructure;

public static class AuditContextFactory
{
    public static AuditContext FromHttpContext(HttpContext context)
    {
        var userId = UserClaims.GetUserId(context.User);
        var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
        var correlationId = context.TraceIdentifier;
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();

        return new AuditContext(userId, role, correlationId, ipAddress, userAgent);
    }
}
