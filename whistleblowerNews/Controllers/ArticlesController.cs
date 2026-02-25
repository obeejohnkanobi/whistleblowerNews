using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using whistleblowerNews.Application.Articles;
using whistleblowerNews.Authorization;
using whistleblowerNews.Domain;
using whistleblowerNews.Infrastructure;

namespace whistleblowerNews.Controllers;

[ApiController]
[Route("articles")]
public sealed class ArticlesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthorizationService _authorization;

    public ArticlesController(ApplicationDbContext db, IAuthorizationService authorization)
    {
        _db = db;
        _authorization = authorization;
    }

    // GET /articles (public)
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ArticleDto>>> GetAll(CancellationToken ct)
    {
        var items = await _db.Articles
            .AsNoTracking()
            .Include(a => a.Author)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ArticleDto(
                a.Id,
                a.Title,
                a.Content,
                a.AuthorId,
                a.Author.Username,
                a.CreatedAt))
            .ToListAsync(ct);

        return Ok(items);
    }

    // GET /articles/{id} (public)
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArticleDto>> GetById(int id, CancellationToken ct)
    {
        var article = await _db.Articles
            .AsNoTracking()
            .Include(a => a.Author)
            .SingleOrDefaultAsync(a => a.Id == id, ct);

        if (article is null)
            return NotFound();

        var dto = new ArticleDto(
            article.Id,
            article.Title,
            article.Content,
            article.AuthorId,
            article.Author.Username,
            article.CreatedAt);

        return Ok(dto);
    }

    // POST /articles (requires Writer)
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.IsWriter)]
    public async Task<ActionResult<ArticleDto>> Create(
        [FromBody] CreateArticleRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Title and content are required.");

        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var article = new Article(request.Title.Trim(), request.Content.Trim(), userId.Value);
        _db.Articles.Add(article);
        await _db.SaveChangesAsync(ct);

        var author = await _db.Users.AsNoTracking().SingleAsync(u => u.Id == userId.Value, ct);

        var dto = new ArticleDto(
            article.Id,
            article.Title,
            article.Content,
            article.AuthorId,
            author.Username,
            article.CreatedAt);

        return CreatedAtAction(nameof(GetById), new { id = article.Id }, dto);
    }

    // PUT /articles/{id} (Writer owner OR Editor)
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ArticleDto>> Update(
        int id,
        [FromBody] UpdateArticleRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Title and content are required.");

        var article = await _db.Articles
            .Include(a => a.Author)
            .SingleOrDefaultAsync(a => a.Id == id, ct);

        if (article is null)
            return NotFound();

        var authorized = await _authorization.AuthorizeAsync(
            User,
            article,
            AuthorizationPolicies.ArticleOwnerOrEditor);

        if (!authorized.Succeeded)
            return Forbid();

        article.Update(request.Title.Trim(), request.Content.Trim());
        await _db.SaveChangesAsync(ct);

        var dto = new ArticleDto(
            article.Id,
            article.Title,
            article.Content,
            article.AuthorId,
            article.Author.Username,
            article.CreatedAt);

        return Ok(dto);
    }

    // DELETE /articles/{id} (Writer owner OR Editor)
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var article = await _db.Articles.SingleOrDefaultAsync(a => a.Id == id, ct);
        if (article is null)
            return NotFound();

        var authorized = await _authorization.AuthorizeAsync(
            User,
            article,
            AuthorizationPolicies.ArticleOwnerOrEditor);

        if (!authorized.Succeeded)
            return Forbid();

        _db.Articles.Remove(article);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private int? GetUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}