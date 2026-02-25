using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using whistleblowerNews.Domain;
using whistleblowerNews.Infrastructure;
using whistleblowerNews.Services;

namespace whistleblowerNews.Tests;

public static class TestData
{
    public static async Task<(User user, string token)> CreateUserWithTokenAsync(
        TestWebApplicationFactory factory,
        UserRole role,
        string? username = null)
    {
        username ??= $"{role.ToString().ToLower()}_{Guid.NewGuid():N}";

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwt = scope.ServiceProvider.GetRequiredService<JwtTokenService>();

        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.HashPassword("password", salt);

        var user = new User(username, $"{salt}${hash}", role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (token, _) = jwt.CreateToken(user);
        return (user, token);
    }

    public static async Task<Article> CreateArticleAsync(
        TestWebApplicationFactory factory,
        int authorId,
        string? title = null,
        string? content = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var article = new Article(
            title ?? "Test Title",
            content ?? "Test Content",
            authorId);

        db.Articles.Add(article);
        await db.SaveChangesAsync();

        return article;
    }

    public static async Task<Comment> CreateCommentAsync(
        TestWebApplicationFactory factory,
        int articleId,
        int userId,
        string? content = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var comment = new Comment(content ?? "Test Comment", articleId, userId);
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        return comment;
    }

    public static HttpClient CreateAuthenticatedClient(
        TestWebApplicationFactory factory,
        string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}