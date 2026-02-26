using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Application.Articles;
using WhistleblowerNews.Application.Authorization;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Web.Infrastructure;

namespace WhistleblowerNews.Web.Controllers.Api;

[ApiController]
[Route("api/articles")]
public sealed class ArticlesController : ControllerBase
{
    private readonly ArticleService _articles;

    public ArticlesController(ArticleService articles)
    {
        _articles = articles;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ArticleDto>>> GetAll(CancellationToken ct)
    {
        var items = await _articles.GetAllAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArticleDto>> GetById(int id, CancellationToken ct)
    {
        var dto = await _articles.GetByIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.IsWriter)]
    public async Task<ActionResult<ArticleDto>> Create(
        [FromBody] CreateArticleRequest request,
        CancellationToken ct)
    {
        var result = await _articles.CreateAsync(User, request, ct);

        return result.Status switch
        {
            ResultStatus.Created => CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value),
            ResultStatus.BadRequest => BadRequest(result.Error),
            ResultStatus.Unauthorized => Unauthorized(),
            ResultStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ArticleDto>> Update(
        int id,
        [FromBody] UpdateArticleRequest request,
        CancellationToken ct)
    {
        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _articles.UpdateAsync(User, id, request, auditContext, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _articles.DeleteAsync(User, id, auditContext, ct);
        return ToActionResult(result);
    }

    private ActionResult<T> ToActionResult<T>(ServiceResult<T> result)
    {
        return result.Status switch
        {
            ResultStatus.Ok => Ok(result.Value),
            ResultStatus.BadRequest => BadRequest(result.Error),
            ResultStatus.NotFound => NotFound(),
            ResultStatus.Unauthorized => Unauthorized(),
            ResultStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private IActionResult ToActionResult(ServiceResult result)
    {
        return result.Status switch
        {
            ResultStatus.NoContent => NoContent(),
            ResultStatus.BadRequest => BadRequest(result.Error),
            ResultStatus.NotFound => NotFound(),
            ResultStatus.Unauthorized => Unauthorized(),
            ResultStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
