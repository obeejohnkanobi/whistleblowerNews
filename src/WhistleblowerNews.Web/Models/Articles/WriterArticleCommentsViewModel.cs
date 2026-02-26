using WhistleblowerNews.Application.Articles;
using WhistleblowerNews.Application.Comments;

namespace WhistleblowerNews.Web.Models.Articles;

public sealed class WriterArticleCommentsViewModel
{
    public ArticleDto Article { get; init; } = null!;
    public IReadOnlyList<CommentDto> Comments { get; init; } = Array.Empty<CommentDto>();
}
