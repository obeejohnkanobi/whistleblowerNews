using System.Security.Claims;

namespace WhistleblowerNews.Application.Authorization;

internal static class AuthorizationHelpers
{
    public static int? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}
