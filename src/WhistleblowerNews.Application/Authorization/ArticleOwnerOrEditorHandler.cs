using Microsoft.AspNetCore.Authorization;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Application.Authorization;

public sealed class ArticleOwnerOrEditorRequirement : IAuthorizationRequirement
{
}

public sealed class ArticleOwnerOrEditorHandler
    : AuthorizationHandler<ArticleOwnerOrEditorRequirement, Article>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ArticleOwnerOrEditorRequirement requirement,
        Article resource)
    {
        if (context.User.IsInRole(UserRoles.Editor))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!context.User.IsInRole(UserRoles.Writer))
            return Task.CompletedTask;

        var userId = AuthorizationHelpers.GetUserId(context.User);
        if (userId.HasValue && resource.AuthorId == userId.Value)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
