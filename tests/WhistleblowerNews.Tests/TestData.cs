using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using WhistleblowerNews.Application.Authentication;
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
        password ??= "password1";

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        var roleName = role.ToRoleName();
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole<int>(roleName));

        var user = new User
        {
            UserName = username,
            Email = $"{username}@example.local",
            EmailConfirmed = true,
            LockoutEnabled = true
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, roleName);

        return user;
    }

    public static async Task<(User user, HttpClient client)> CreateUserWithClientAsync(
        TestWebApplicationFactory factory,
        UserRole role,
        string? username = null,
        string? password = null)
    {
        password ??= "password1";
        var user = await CreateUserAsync(factory, role, username, password);

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(user.UserName ?? string.Empty, password));

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

    public static async Task LockoutUserAsync(TestWebApplicationFactory factory, User user)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // Reload the user to avoid EF tracking conflicts
        var reloadedUser = await userManager.FindByIdAsync(user.Id.ToString());
        if (reloadedUser is null)
            throw new InvalidOperationException($"User with ID {user.Id} not found.");

        await userManager.SetLockoutEndDateAsync(reloadedUser, DateTimeOffset.UtcNow.AddMinutes(10));
    }

    public static async Task<User> CreateUnconfirmedUserAsync(
        TestWebApplicationFactory factory,
        UserRole role,
        string? username = null,
        string? password = null)
    {
        username ??= $"{role.ToString().ToLower()}_{Guid.NewGuid():N}";
        password ??= "password1";

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        var roleName = role.ToRoleName();
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole<int>(roleName));

        var user = new User
        {
            UserName = username,
            Email = $"{username}@example.local",
            EmailConfirmed = false,
            LockoutEnabled = true
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, roleName);

        return user;
    }
}
