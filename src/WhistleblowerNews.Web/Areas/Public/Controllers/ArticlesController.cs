using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Application.Articles;
using WhistleblowerNews.Application.Comments;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Web.Models.Articles;

namespace WhistleblowerNews.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
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
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        const int pageSize = 10;
        var items = await _articles.GetAllAsync(ct);
        var totalCount = items.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var safeTotalPages = Math.Max(1, totalPages);
        page = Math.Clamp(page, 1, safeTotalPages);

        var paged = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var model = new ArticleListViewModel
        {
            Articles = paged,
            Page = page,
            TotalPages = safeTotalPages,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var article = await _articles.GetByIdAsync(id, ct);
        if (article is null)
            return NotFound();

        var commentsResult = await _comments.GetCommentsForArticleAsync(id, ct);
        if (commentsResult.Status == ResultStatus.NotFound)
            return NotFound();

        var model = new ArticleDetailsViewModel
        {
            Article = article,
            Comments = commentsResult.Value ?? Array.Empty<Application.Comments.CommentDto>()
        };

        return View(model);
    }
}
