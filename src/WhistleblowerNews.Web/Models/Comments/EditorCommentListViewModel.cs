namespace WhistleblowerNews.Web.Models.Comments;

public sealed record EditorCommentItemViewModel(
    int Id,
    int ArticleId,
    string ArticleTitle,
    string Username,
    string Content,
    DateTime CreatedAt);

public sealed class EditorCommentListViewModel
{
    public IReadOnlyList<EditorCommentItemViewModel> Comments { get; init; }
        = Array.Empty<EditorCommentItemViewModel>();

    public string? Username { get; init; }
    public string? ArticleTitle { get; init; }
}
