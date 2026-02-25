using Microsoft.AspNetCore.Authorization;
using whistleblowerNews.Domain;

namespace whistleblowerNews.Authorization;

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
        if (context.User.IsInRole(UserRole.Editor.ToString()))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!context.User.IsInRole(UserRole.Writer.ToString()))
            return Task.CompletedTask;

        var userId = AuthorizationHelpers.GetUserId(context.User);
        if (userId.HasValue && resource.AuthorId == userId.Value)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}