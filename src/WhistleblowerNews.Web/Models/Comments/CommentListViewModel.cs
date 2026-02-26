namespace WhistleblowerNews.Web.Models.Comments;

public sealed class CommentListViewModel
{
    public IReadOnlyList<WhistleblowerNews.Application.Comments.CommentDto> Comments { get; init; }
        = Array.Empty<WhistleblowerNews.Application.Comments.CommentDto>();

    public IReadOnlyList<int> ArticleIds { get; init; } = Array.Empty<int>();
    public int? SelectedArticleId { get; init; }
}
