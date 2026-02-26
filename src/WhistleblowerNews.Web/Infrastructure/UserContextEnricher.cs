using System.Security.Claims;
using Serilog.Core;
using Serilog.Events;

namespace WhistleblowerNews.Web.Infrastructure;

public sealed class UserContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return;

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("UserId", userId));

        var roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value).Distinct().ToArray();
        if (roles.Length > 0)
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("Roles", roles));
    }
}
