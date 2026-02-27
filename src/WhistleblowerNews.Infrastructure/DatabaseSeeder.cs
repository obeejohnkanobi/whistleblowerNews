using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Infrastructure;

/// <summary>
/// Seeds the database with initial users for development/testing.
/// Only run in Development.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        UserManager<User> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        CancellationToken ct = default)
    {
        // Ensure schema is up to date (applies migrations)
        await db.Database.MigrateAsync(ct);

        // If we already have users, do nothing (idempotent seed).
        if (await userManager.Users.AnyAsync(ct))
            return;

        // Ensure roles exist
        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<int>(role));
        }

        // Create deterministic demo users (for teacher demos and tests)
        // NOTE: Passwords are simple on purpose (demo). We'll improve later.
        await CreateUserAsync(userManager, "subscriber", "subscriber123", UserRole.Subscriber);
        await CreateUserAsync(userManager, "writer", "writer123", UserRole.Writer);
        await CreateUserAsync(userManager, "editor", "editor123", UserRole.Editor);
        await CreateUserAsync(userManager, "investigator", "investigator123", UserRole.Investigator);
    }

    private static async Task CreateUserAsync(
        UserManager<User> userManager,
        string username,
        string plainPassword,
        UserRole role)
    {
        var user = new User
        {
            UserName = username,
            Email = $"{username}@example.local",
            EmailConfirmed = true,
            LockoutEnabled = true
        };
        var result = await userManager.CreateAsync(user, plainPassword);
        if (!result.Succeeded)
            return;

        await userManager.AddToRoleAsync(user, role.ToString());
    }
}

