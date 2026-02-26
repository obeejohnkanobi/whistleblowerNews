using System.Security.Claims;

namespace WhistleblowerNews.Web.Infrastructure;

public static class UserClaims
{
    public static int? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}
