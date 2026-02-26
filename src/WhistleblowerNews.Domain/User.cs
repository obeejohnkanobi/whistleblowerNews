namespace WhistleblowerNews.Domain;

/// <summary>
/// Represents an authenticated user in the system.
/// </summary>
public class User
{
    public int Id { get; private set; }

    public string Username { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    // Navigation properties
    public ICollection<Article> Articles { get; private set; } = new List<Article>();
    public ICollection<Comment> Comments { get; private set; } = new List<Comment>();

    // Required by EF Core
    private User() { }

    public User(string username, string passwordHash, UserRole role)
    {
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
    }
}
