using WhistleblowerNews.Application.Articles;

namespace WhistleblowerNews.Web.Models.Articles;

public sealed class ArticleListViewModel
{
    public IReadOnlyList<ArticleDto> Articles { get; init; } = Array.Empty<ArticleDto>();
    public int Page { get; init; }
    public int TotalPages { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
