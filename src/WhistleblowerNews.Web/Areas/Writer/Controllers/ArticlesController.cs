using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Application.Articles;
using WhistleblowerNews.Application.Comments;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Web.Infrastructure;
using WhistleblowerNews.Web.Models.Articles;

namespace WhistleblowerNews.Web.Areas.Writer.Controllers;

[Area("Writer")]
[Authorize(Roles = "Writer")]
public sealed class ArticlesController : Controller
{
    private readonly ArticleService _articles;
    private readonly CommentService _comments;

    public ArticlesController(ArticleService articles, CommentService comments)
    {
        _articles = articles;
        _comments = comments;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = UserClaims.GetUserId(User);
        if (!userId.HasValue)
            return Unauthorized();

        var items = await _articles.GetByAuthorAsync(userId.Value, ct);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Comments(int id, CancellationToken ct)
    {
        var userId = UserClaims.GetUserId(User);
        if (!userId.HasValue)
            return Unauthorized();

        var article = await _articles.GetByIdAsync(id, ct);
        if (article is null)
            return NotFound();

        if (article.AuthorId != userId.Value)
            return Forbid();

        var commentsResult = await _comments.GetCommentsForArticleAsync(id, ct);
        if (commentsResult.Status == ResultStatus.NotFound)
            return NotFound();

        var model = new WriterArticleCommentsViewModel
        {
            Article = article,
            Comments = commentsResult.Value ?? Array.Empty<Application.Comments.CommentDto>()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ArticleFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticleFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _articles.CreateAsync(User, new CreateArticleRequest(model.Title, model.Content), auditContext, ct);
        if (result.Status == ResultStatus.Created)
            return RedirectToAction(nameof(Index));

        if (result.Status == ResultStatus.Forbidden)
            return Forbid();

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create article.");
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
