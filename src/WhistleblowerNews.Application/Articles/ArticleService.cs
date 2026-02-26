using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WhistleblowerNews.Application.Auditing;
using WhistleblowerNews.Application.Abstractions;
using WhistleblowerNews.Application.Authorization;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Application.Articles;

public sealed class ArticleService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuthorizationService _authorization;
    private readonly IAuditService _audit;

    public ArticleService(IApplicationDbContext db, IAuthorizationService authorization, IAuditService audit)
    {
        _db = db;
        _authorization = authorization;
        _audit = audit;
    }

    public async Task<IReadOnlyList<ArticleDto>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Articles
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
    }

    public async Task<ArticleDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var article = await _db.Articles
            .AsNoTracking()
            .Include(a => a.Author)
            .SingleOrDefaultAsync(a => a.Id == id, ct);

        if (article is null)
            return null;

        return new ArticleDto(
            article.Id,
            article.Title,
            article.Content,
            article.AuthorId,
            article.Author.Username,
            article.CreatedAt);
    }

    public async Task<ServiceResult<ArticleDto>> CreateAsync(
        ClaimsPrincipal user,
        CreateArticleRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return ServiceResult<ArticleDto>.BadRequest("Title and content are required.");

        var auth = await _authorization.AuthorizeAsync(user, null, AuthorizationPolicies.IsWriter);
        if (!auth.Succeeded)
            return AuthFailure<ArticleDto>(user);

        var userId = AuthorizationHelpers.GetUserId(user);
        if (!userId.HasValue)
            return ServiceResult<ArticleDto>.Unauthorized("Authentication required.");

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

        return ServiceResult<ArticleDto>.Created(dto);
    }

    public async Task<ServiceResult<ArticleDto>> UpdateAsync(
        ClaimsPrincipal user,
        int id,
        UpdateArticleRequest request,
        AuditContext auditContext,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return ServiceResult<ArticleDto>.BadRequest("Title and content are required.");

        var article = await _db.Articles
            .Include(a => a.Author)
            .SingleOrDefaultAsync(a => a.Id == id, ct);

        if (article is null)
            return ServiceResult<ArticleDto>.NotFound("Article not found.");

        var auth = await _authorization.AuthorizeAsync(user, article, AuthorizationPolicies.ArticleOwnerOrEditor);
        if (!auth.Succeeded)
            return AuthFailure<ArticleDto>(user);

        article.Update(request.Title.Trim(), request.Content.Trim());
        await _db.SaveChangesAsync(ct);

        await _audit.Success(
            AuditEventType.ArticleUpdated,
            "Article",
            article.Id.ToString(),
            null,
            auditContext,
            ct);

        var dto = new ArticleDto(
            article.Id,
            article.Title,
            article.Content,
            article.AuthorId,
            article.Author.Username,
            article.CreatedAt);

        return ServiceResult<ArticleDto>.Ok(dto);
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        int id,
        AuditContext auditContext,
        CancellationToken ct)
    {
        var article = await _db.Articles.SingleOrDefaultAsync(a => a.Id == id, ct);
        if (article is null)
            return ServiceResult.NotFound("Article not found.");

        var auth = await _authorization.AuthorizeAsync(user, article, AuthorizationPolicies.ArticleOwnerOrEditor);
        if (!auth.Succeeded)
            return AuthFailure(user);

        _db.Articles.Remove(article);
        await _db.SaveChangesAsync(ct);

        await _audit.Success(
            AuditEventType.ArticleDeleted,
            "Article",
            article.Id.ToString(),
            null,
            auditContext,
            ct);

        return ServiceResult.NoContent();
    }

    public async Task<IReadOnlyList<ArticleDto>> GetByAuthorAsync(int authorId, CancellationToken ct)
    {
        return await _db.Articles
            .AsNoTracking()
            .Include(a => a.Author)
            .Where(a => a.AuthorId == authorId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ArticleDto(
                a.Id,
                a.Title,
                a.Content,
                a.AuthorId,
                a.Author.Username,
                a.CreatedAt))
            .ToListAsync(ct);
    }

    private static ServiceResult<T> AuthFailure<T>(ClaimsPrincipal user)
    {
        return IsAuthenticated(user)
            ? ServiceResult<T>.Forbidden("Forbidden.")
            : ServiceResult<T>.Unauthorized("Authentication required.");
    }

    private static ServiceResult AuthFailure(ClaimsPrincipal user)
    {
        return IsAuthenticated(user)
            ? ServiceResult.Forbidden("Forbidden.")
            : ServiceResult.Unauthorized("Authentication required.");
    }

    private static bool IsAuthenticated(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true;
}
