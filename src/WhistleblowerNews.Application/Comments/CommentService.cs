using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WhistleblowerNews.Application.Auditing;
using WhistleblowerNews.Application.Abstractions;
using WhistleblowerNews.Application.Authorization;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Application.Comments;

public sealed class CommentService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuthorizationService _authorization;
    private readonly IAuditService _audit;

    public CommentService(IApplicationDbContext db, IAuthorizationService authorization, IAuditService audit)
    {
        _db = db;
        _authorization = authorization;
        _audit = audit;
    }

    public async Task<ServiceResult<IReadOnlyList<CommentDto>>> GetCommentsForArticleAsync(
        int articleId,
        CancellationToken ct)
    {
        var articleExists = await _db.Articles.AnyAsync(a => a.Id == articleId, ct);
        if (!articleExists)
            return ServiceResult<IReadOnlyList<CommentDto>>.NotFound("Article not found.");

        var comments = await _db.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.ArticleId == articleId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentDto(
                c.Id,
                c.Content,
                c.ArticleId,
                c.UserId,
                c.User.Username,
                c.CreatedAt))
            .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<CommentDto>>.Ok(comments);
    }

    public async Task<IReadOnlyList<CommentDto>> GetCommentsByUserAsync(int userId, CancellationToken ct)
    {
        return await _db.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentDto(
                c.Id,
                c.Content,
                c.ArticleId,
                c.UserId,
                c.User.Username,
                c.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CommentDto>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentDto(
                c.Id,
                c.Content,
                c.ArticleId,
                c.UserId,
                c.User.Username,
                c.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<CommentDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var comment = await _db.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .SingleOrDefaultAsync(c => c.Id == id, ct);

        if (comment is null)
            return null;

        return new CommentDto(
            comment.Id,
            comment.Content,
            comment.ArticleId,
            comment.UserId,
            comment.User.Username,
            comment.CreatedAt);
    }

    public async Task<ServiceResult<CommentDto>> CreateAsync(
        ClaimsPrincipal user,
        int articleId,
        CreateCommentRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return ServiceResult<CommentDto>.BadRequest("Content is required.");

        var articleExists = await _db.Articles.AnyAsync(a => a.Id == articleId, ct);
        if (!articleExists)
            return ServiceResult<CommentDto>.NotFound("Article not found.");

        var auth = await _authorization.AuthorizeAsync(user, null, AuthorizationPolicies.IsSubscriber);
        if (!auth.Succeeded)
            return AuthFailure<CommentDto>(user);

        var userId = AuthorizationHelpers.GetUserId(user);
        if (!userId.HasValue)
            return ServiceResult<CommentDto>.Unauthorized("Authentication required.");

        var comment = new Comment(request.Content.Trim(), articleId, userId.Value);
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);

        var author = await _db.Users.AsNoTracking().SingleAsync(u => u.Id == userId.Value, ct);

        var dto = new CommentDto(
            comment.Id,
            comment.Content,
            comment.ArticleId,
            comment.UserId,
            author.Username,
            comment.CreatedAt);

        return ServiceResult<CommentDto>.Created(dto);
    }

    public async Task<ServiceResult<CommentDto>> UpdateAsync(
        ClaimsPrincipal user,
        int commentId,
        UpdateCommentRequest request,
        AuditContext auditContext,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return ServiceResult<CommentDto>.BadRequest("Content is required.");

        var comment = await _db.Comments
            .Include(c => c.User)
            .SingleOrDefaultAsync(c => c.Id == commentId, ct);

        if (comment is null)
            return ServiceResult<CommentDto>.NotFound("Comment not found.");

        var auth = await _authorization.AuthorizeAsync(user, comment, AuthorizationPolicies.CommentOwnerOrEditor);
        if (!auth.Succeeded)
            return AuthFailure<CommentDto>(user);

        comment.UpdateContent(request.Content.Trim());
        await _db.SaveChangesAsync(ct);

        await _audit.Success(
            AuditEventType.CommentUpdated,
            "Comment",
            comment.Id.ToString(),
            null,
            auditContext,
            ct);

        var dto = new CommentDto(
            comment.Id,
            comment.Content,
            comment.ArticleId,
            comment.UserId,
            comment.User.Username,
            comment.CreatedAt);

        return ServiceResult<CommentDto>.Ok(dto);
    }

    public async Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        int commentId,
        AuditContext auditContext,
        CancellationToken ct)
    {
        var comment = await _db.Comments.SingleOrDefaultAsync(c => c.Id == commentId, ct);
        if (comment is null)
            return ServiceResult.NotFound("Comment not found.");

        var auth = await _authorization.AuthorizeAsync(user, comment, AuthorizationPolicies.CommentOwnerOrEditor);
        if (!auth.Succeeded)
            return AuthFailure(user);

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync(ct);

        await _audit.Success(
            AuditEventType.CommentDeleted,
            "Comment",
            comment.Id.ToString(),
            null,
            auditContext,
            ct);

        return ServiceResult.NoContent();
    }

    public async Task<ServiceResult> DeleteCommentOnArticleAsync(
        ClaimsPrincipal user,
        int articleId,
        int commentId,
        AuditContext auditContext,
        CancellationToken ct)
    {
        var comment = await _db.Comments
            .Include(c => c.Article)
            .SingleOrDefaultAsync(c => c.Id == commentId && c.ArticleId == articleId, ct);

        if (comment is null)
            return ServiceResult.NotFound("Comment not found.");

        var auth = await _authorization.AuthorizeAsync(user, comment.Article, AuthorizationPolicies.WriterOwnsArticle);
        if (!auth.Succeeded)
            return AuthFailure(user);

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync(ct);

        await _audit.Success(
            AuditEventType.CommentDeleted,
            "Comment",
            comment.Id.ToString(),
            null,
            auditContext,
            ct);

        return ServiceResult.NoContent();
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
