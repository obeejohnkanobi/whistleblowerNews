namespace WhistleblowerNews.Application.Comments;

public sealed record CommentDto(
    int Id,
    string Content,
    int ArticleId,
    int UserId,
    string Username,
    DateTime CreatedAt);

public sealed record CreateCommentRequest(string Content);

public sealed record UpdateCommentRequest(string Content);
