using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using WhistleblowerNews.Application.Authentication;
using WhistleblowerNews.Application.Services;
using WhistleblowerNews.Domain;
using WhistleblowerNews.Infrastructure;

namespace WhistleblowerNews.Tests;

public static class TestData
{
    public static async Task<User> CreateUserAsync(
        TestWebApplicationFactory factory,
        UserRole role,
        string? username = null,
        string? password = null)
    {
        username ??= $"{role.ToString().ToLower()}_{Guid.NewGuid():N}";
        password ??= "password";

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.HashPassword(password, salt);

        var user = new User(username, $"{salt}${hash}", role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    public static async Task<(User user, HttpClient client)> CreateUserWithClientAsync(
        TestWebApplicationFactory factory,
        UserRole role,
        string? username = null,
        string? password = null)
    {
        password ??= "password";
        var user = await CreateUserAsync(factory, role, username, password);

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(user.Username, password));

        response.EnsureSuccessStatusCode();

        return (user, client);
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
}
