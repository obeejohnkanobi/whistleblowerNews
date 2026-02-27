using Microsoft.AspNetCore.Identity;

namespace WhistleblowerNews.Domain;

/// <summary>
/// Represents an authenticated user in the system.
/// </summary>
public class User : IdentityUser<int>
{
    // Navigation properties
    public ICollection<Article> Articles { get; private set; } = new List<Article>();
    public ICollection<Comment> Comments { get; private set; } = new List<Comment>();
}
