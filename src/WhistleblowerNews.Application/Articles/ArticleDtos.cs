namespace WhistleblowerNews.Application.Articles;

public sealed record ArticleDto(
    int Id,
    string Title,
    string Content,
    int AuthorId,
    string AuthorUsername,
    DateTime CreatedAt);

public sealed record CreateArticleRequest(string Title, string Content);

public sealed record UpdateArticleRequest(string Title, string Content);
