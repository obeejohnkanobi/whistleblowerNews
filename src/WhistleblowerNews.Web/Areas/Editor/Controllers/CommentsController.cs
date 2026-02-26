using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Application.Articles;
using WhistleblowerNews.Application.Comments;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Web.Models.Comments;
using WhistleblowerNews.Web.Infrastructure;

namespace WhistleblowerNews.Web.Areas.Editor.Controllers;

[Area("Editor")]
[Authorize(Roles = "Editor")]
public sealed class CommentsController : Controller
{
    private readonly CommentService _comments;
    private readonly ArticleService _articles;

    public CommentsController(CommentService comments, ArticleService articles)
    {
        _comments = comments;
        _articles = articles;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? username, string? articleTitle, CancellationToken ct)
    {
        var items = await _comments.GetAllAsync(ct);
        var articles = await _articles.GetAllAsync(ct);
        var titlesById = articles.ToDictionary(a => a.Id, a => a.Title);

        var viewItems = items
            .Select(c => new EditorCommentItemViewModel(
                c.Id,
                c.ArticleId,
                titlesById.TryGetValue(c.ArticleId, out var title) ? title : $"Article {c.ArticleId}",
                c.Username,
                c.Content,
                c.CreatedAt))
            .ToList();

        if (!string.IsNullOrWhiteSpace(username))
        {
            viewItems = viewItems
                .Where(c => c.Username.Contains(username, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(articleTitle))
        {
            viewItems = viewItems
                .Where(c => c.ArticleTitle.Contains(articleTitle, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        viewItems = viewItems
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var model = new EditorCommentListViewModel
        {
            Comments = viewItems,
            Username = username,
            ArticleTitle = articleTitle
        };

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

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update comment.");
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
