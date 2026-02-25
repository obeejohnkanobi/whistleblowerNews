using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using whistleblowerNews.Application.Comments;
using whistleblowerNews.Authorization;
using whistleblowerNews.Domain;
using whistleblowerNews.Infrastructure;

namespace whistleblowerNews.Controllers;

[ApiController]
public sealed class CommentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthorizationService _authorization;

    public CommentsController(ApplicationDbContext db, IAuthorizationService authorization)
    {
        _db = db;
        _authorization = authorization;
    }

    // GET /articles/{id}/comments (public)
    [HttpGet("/articles/{articleId:int}/comments")]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetCommentsForArticle(
        int articleId,
        CancellationToken ct)
    {
        var articleExists = await _db.Articles.AnyAsync(a => a.Id == articleId, ct);
        if (!articleExists)
            return NotFound();

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

        return Ok(comments);
    }

    // POST /articles/{id}/comments (Subscriber)
    [HttpPost("/articles/{articleId:int}/comments")]
    [Authorize(Policy = AuthorizationPolicies.IsSubscriber)]
    public async Task<ActionResult<CommentDto>> CreateComment(
        int articleId,
        [FromBody] CreateCommentRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Content is required.");

        var articleExists = await _db.Articles.AnyAsync(a => a.Id == articleId, ct);
        if (!articleExists)
            return NotFound();

        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var comment = new Comment(request.Content.Trim(), articleId, userId.Value);
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);

        var user = await _db.Users.AsNoTracking().SingleAsync(u => u.Id == userId.Value, ct);

        var dto = new CommentDto(
            comment.Id,
            comment.Content,
            comment.ArticleId,
            comment.UserId,
            user.Username,
            comment.CreatedAt);

        return CreatedAtAction(nameof(GetCommentsForArticle), new { articleId }, dto);
    }

    // PUT /comments/{id} (Subscriber owner OR Editor)
    [HttpPut("/comments/{id:int}")]
    [Authorize]
    public async Task<ActionResult<CommentDto>> UpdateComment(
        int id,
        [FromBody] UpdateCommentRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Content is required.");

        var comment = await _db.Comments
            .Include(c => c.User)
            .SingleOrDefaultAsync(c => c.Id == id, ct);

        if (comment is null)
            return NotFound();

        var authorized = await _authorization.AuthorizeAsync(
            User,
            comment,
            AuthorizationPolicies.CommentOwnerOrEditor);

        if (!authorized.Succeeded)
            return Forbid();

        comment.UpdateContent(request.Content.Trim());
        await _db.SaveChangesAsync(ct);

        var dto = new CommentDto(
            comment.Id,
            comment.Content,
            comment.ArticleId,
            comment.UserId,
            comment.User.Username,
            comment.CreatedAt);

        return Ok(dto);
    }

    // DELETE /comments/{id} (Subscriber owner OR Editor)
    [HttpDelete("/comments/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(int id, CancellationToken ct)
    {
        var comment = await _db.Comments.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (comment is null)
            return NotFound();

        var authorized = await _authorization.AuthorizeAsync(
            User,
            comment,
            AuthorizationPolicies.CommentOwnerOrEditor);

        if (!authorized.Succeeded)
            return Forbid();

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // DELETE /articles/{articleId}/comments/{commentId} (Writer if article owner)
    [HttpDelete("/articles/{articleId:int}/comments/{commentId:int}")]
    [Authorize(Policy = AuthorizationPolicies.IsWriter)]
    public async Task<IActionResult> DeleteCommentForArticle(
        int articleId,
        int commentId,
        CancellationToken ct)
    {
        var comment = await _db.Comments
            .Include(c => c.Article)
            .SingleOrDefaultAsync(c => c.Id == commentId && c.ArticleId == articleId, ct);

        if (comment is null)
            return NotFound();

        var authorized = await _authorization.AuthorizeAsync(
            User,
            comment.Article,
            AuthorizationPolicies.WriterOwnsArticle);

        if (!authorized.Succeeded)
            return Forbid();

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private int? GetUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}