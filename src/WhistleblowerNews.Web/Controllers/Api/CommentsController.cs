using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Application.Authorization;
using WhistleblowerNews.Application.Comments;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Web.Infrastructure;

namespace WhistleblowerNews.Web.Controllers.Api;

[ApiController]
[Route("api")]
public sealed class CommentsController : ControllerBase
{
    private readonly CommentService _comments;

    public CommentsController(CommentService comments)
    {
        _comments = comments;
    }

    [HttpGet("articles/{articleId:int}/comments")]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetCommentsForArticle(
        int articleId,
        CancellationToken ct)
    {
        var result = await _comments.GetCommentsForArticleAsync(articleId, ct);
        return result.Status switch
        {
            ResultStatus.Ok => Ok(result.Value),
            ResultStatus.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost("articles/{articleId:int}/comments")]
    [Authorize(Policy = AuthorizationPolicies.IsSubscriber)]
    public async Task<ActionResult<CommentDto>> CreateComment(
        int articleId,
        [FromBody] CreateCommentRequest request,
        CancellationToken ct)
    {
        var result = await _comments.CreateAsync(User, articleId, request, ct);

        return result.Status switch
        {
            ResultStatus.Created => CreatedAtAction(nameof(GetCommentsForArticle), new { articleId }, result.Value),
            ResultStatus.BadRequest => BadRequest(result.Error),
            ResultStatus.NotFound => NotFound(),
            ResultStatus.Unauthorized => Unauthorized(),
            ResultStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPut("comments/{id:int}")]
    [Authorize]
    public async Task<ActionResult<CommentDto>> UpdateComment(
        int id,
        [FromBody] UpdateCommentRequest request,
        CancellationToken ct)
    {
        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _comments.UpdateAsync(User, id, request, auditContext, ct);
        return ToActionResult(result);
    }

    [HttpDelete("comments/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(int id, CancellationToken ct)
    {
        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _comments.DeleteAsync(User, id, auditContext, ct);
        return ToActionResult(result);
    }

    [HttpDelete("articles/{articleId:int}/comments/{commentId:int}")]
    [Authorize(Policy = AuthorizationPolicies.IsWriter)]
    public async Task<IActionResult> DeleteCommentForArticle(
        int articleId,
        int commentId,
        CancellationToken ct)
    {
        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _comments.DeleteCommentOnArticleAsync(User, articleId, commentId, auditContext, ct);
        return ToActionResult(result);
    }

    private ActionResult<T> ToActionResult<T>(ServiceResult<T> result)
    {
        return result.Status switch
        {
            ResultStatus.Ok => Ok(result.Value),
            ResultStatus.BadRequest => BadRequest(result.Error),
            ResultStatus.NotFound => NotFound(),
            ResultStatus.Unauthorized => Unauthorized(),
            ResultStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private IActionResult ToActionResult(ServiceResult result)
    {
        return result.Status switch
        {
            ResultStatus.NoContent => NoContent(),
            ResultStatus.BadRequest => BadRequest(result.Error),
            ResultStatus.NotFound => NotFound(),
            ResultStatus.Unauthorized => Unauthorized(),
            ResultStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
