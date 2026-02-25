using Microsoft.AspNetCore.Authorization;
using whistleblowerNews.Domain;

namespace whistleblowerNews.Authorization;

public sealed class WriterOwnsArticleRequirement : IAuthorizationRequirement
{
}

public sealed class WriterOwnsArticleHandler
    : AuthorizationHandler<WriterOwnsArticleRequirement, Article>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WriterOwnsArticleRequirement requirement,
        Article resource)
    {
        if (!context.User.IsInRole(UserRole.Writer.ToString()))
            return Task.CompletedTask;

        var userId = AuthorizationHelpers.GetUserId(context.User);
        if (userId.HasValue && resource.AuthorId == userId.Value)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}