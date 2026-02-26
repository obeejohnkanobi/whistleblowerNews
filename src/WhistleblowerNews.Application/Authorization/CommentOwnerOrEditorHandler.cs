using Microsoft.AspNetCore.Authorization;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Application.Authorization;

public sealed class CommentOwnerOrEditorRequirement : IAuthorizationRequirement
{
}

public sealed class CommentOwnerOrEditorHandler
    : AuthorizationHandler<CommentOwnerOrEditorRequirement, Comment>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CommentOwnerOrEditorRequirement requirement,
        Comment resource)
    {
        if (context.User.IsInRole(UserRole.Editor.ToString()))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!context.User.IsInRole(UserRole.Subscriber.ToString()))
            return Task.CompletedTask;

        var userId = AuthorizationHelpers.GetUserId(context.User);
        if (userId.HasValue && resource.UserId == userId.Value)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
