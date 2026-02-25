namespace whistleblowerNews.Domain;

/// <summary>
/// Represents a news article written by a Writer.
/// </summary>
public class Article
{
    public int Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public int AuthorId { get; private set; }

    public User Author { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public ICollection<Comment> Comments { get; private set; } = new List<Comment>();

    private Article() { }

    public Article(string title, string content, int authorId)
    {
        Title = title;
        Content = content;
        AuthorId = authorId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string title, string content)
    {
        Title = title;
        Content = content;
    }
}
