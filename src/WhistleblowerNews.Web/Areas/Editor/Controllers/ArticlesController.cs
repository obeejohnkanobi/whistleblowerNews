using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Application.Articles;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Web.Models.Articles;
using WhistleblowerNews.Web.Infrastructure;

namespace WhistleblowerNews.Web.Areas.Editor.Controllers;

[Area("Editor")]
[Authorize(Roles = "Editor")]
public sealed class ArticlesController : Controller
{
    private readonly ArticleService _articles;

    public ArticlesController(ArticleService articles)
    {
        _articles = articles;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? query, CancellationToken ct)
    {
        var items = await _articles.GetAllAsync(ct);
        if (!string.IsNullOrWhiteSpace(query))
        {
            items = items
                .Where(a =>
                    a.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    a.AuthorUsername.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var model = new EditorArticleListViewModel
        {
            Articles = items,
            Query = query
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var article = await _articles.GetByIdAsync(id, ct);
        if (article is null)
            return NotFound();

        var model = new ArticleFormViewModel { Title = article.Title, Content = article.Content };
        ViewData["ArticleId"] = id;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ArticleFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ArticleId"] = id;
            return View(model);
        }

        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _articles.UpdateAsync(User, id, new UpdateArticleRequest(model.Title, model.Content), auditContext, ct);
        if (result.Status == ResultStatus.Ok)
            return RedirectToAction(nameof(Index));

        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        if (result.Status == ResultStatus.Forbidden)
            return Forbid();

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update article.");
        ViewData["ArticleId"] = id;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _articles.DeleteAsync(User, id, auditContext, ct);
        if (result.Status == ResultStatus.NoContent)
            return RedirectToAction(nameof(Index));

        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        if (result.Status == ResultStatus.Forbidden)
            return Forbid();

        return BadRequest(result.Error);
    }
}
