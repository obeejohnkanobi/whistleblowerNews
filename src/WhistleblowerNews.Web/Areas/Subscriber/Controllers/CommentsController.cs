using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Application.Comments;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Web.Infrastructure;
using WhistleblowerNews.Web.Models.Comments;

namespace WhistleblowerNews.Web.Areas.Subscriber.Controllers;

[Area("Subscriber")]
[Authorize(Roles = "Subscriber")]
public sealed class CommentsController : Controller
{
    private readonly CommentService _comments;

    public CommentsController(CommentService comments)
    {
        _comments = comments;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? articleId, CancellationToken ct)
    {
        var userId = UserClaims.GetUserId(User);
        if (!userId.HasValue)
            return Unauthorized();

        var items = await _comments.GetCommentsByUserAsync(userId.Value, ct);
        var filtered = articleId.HasValue
            ? items.Where(c => c.ArticleId == articleId.Value).ToList()
            : items;

        var ordered = filtered
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var model = new CommentListViewModel
        {
            Comments = ordered,
            SelectedArticleId = articleId,
            ArticleIds = items.Select(c => c.ArticleId).Distinct().OrderBy(id => id).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create(int articleId)
    {
        ViewData["ArticleId"] = articleId;
        return View(new CommentFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int articleId, CommentFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ArticleId"] = articleId;
            return View(model);
        }

        var result = await _comments.CreateAsync(User, articleId, new CreateCommentRequest(model.Content), ct);
        if (result.Status == ResultStatus.Created)
            return RedirectToAction("Details", "Articles", new { area = "Public", id = articleId });

        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        if (result.Status == ResultStatus.Unauthorized)
            return Unauthorized();

        if (result.Status == ResultStatus.Forbidden)
            return Forbid();

        ModelState.AddModelError(string.Empty, result.Error ?? "Unable to add your comment.");
        ViewData["ArticleId"] = articleId;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var comment = await _comments.GetByIdAsync(id, ct);
        if (comment is null)
            return NotFound();

        var model = new CommentFormViewModel { Content = comment.Content };
        ViewData["CommentId"] = id;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CommentFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewData["CommentId"] = id;
            return View(model);
        }

        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _comments.UpdateAsync(User, id, new UpdateCommentRequest(model.Content), auditContext, ct);
        if (result.Status == ResultStatus.Ok)
            return RedirectToAction(nameof(Index));

        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        if (result.Status == ResultStatus.Forbidden)
            return Forbid();

        ModelState.AddModelError(string.Empty, result.Error ?? "Unable to update your comment.");
        ViewData["CommentId"] = id;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _comments.DeleteAsync(User, id, auditContext, ct);
        if (result.Status == ResultStatus.NoContent)
            return RedirectToAction(nameof(Index));

        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        if (result.Status == ResultStatus.Forbidden)
            return Forbid();

        return BadRequest(result.Error);
    }
}
