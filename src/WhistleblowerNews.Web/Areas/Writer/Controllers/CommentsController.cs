using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Application.Comments;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Web.Infrastructure;

namespace WhistleblowerNews.Web.Areas.Writer.Controllers;

[Area("Writer")]
[Authorize(Roles = "Writer")]
public sealed class CommentsController : Controller
{
    private readonly CommentService _comments;

    public CommentsController(CommentService comments)
    {
        _comments = comments;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int articleId, int commentId, CancellationToken ct)
    {
        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _comments.DeleteCommentOnArticleAsync(User, articleId, commentId, auditContext, ct);
        if (result.Status == ResultStatus.NoContent)
            return RedirectToAction("Comments", "Articles", new { area = "Writer", id = articleId });

        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        if (result.Status == ResultStatus.Forbidden)
            return Forbid();

        return BadRequest(result.Error);
    }
}
