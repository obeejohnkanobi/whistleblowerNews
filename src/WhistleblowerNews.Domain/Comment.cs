namespace WhistleblowerNews.Domain;

/// <summary>
/// Represents a comment made by a Subscriber on an Article.
/// </summary>
public class Comment
{
    public int Id { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public int ArticleId { get; private set; }
    public Article Article { get; private set; } = null!;

    public int UserId { get; private set; }
    public User User { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    private Comment() { }

    public Comment(string content, int articleId, int userId)
    {
        Content = content;
        ArticleId = articleId;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateContent(string content)
    {
        Content = content;
    }
}

