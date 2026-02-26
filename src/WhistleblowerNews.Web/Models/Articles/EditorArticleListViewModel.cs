using WhistleblowerNews.Application.Articles;

namespace WhistleblowerNews.Web.Models.Articles;

public sealed class EditorArticleListViewModel
{
    public IReadOnlyList<ArticleDto> Articles { get; init; } = Array.Empty<ArticleDto>();
    public string? Query { get; init; }
}
