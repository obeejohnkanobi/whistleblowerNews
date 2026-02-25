using Microsoft.EntityFrameworkCore;
using whistleblowerNews.Domain;
using whistleblowerNews.Services;

namespace whistleblowerNews.Infrastructure;

/// <summary>
/// Seeds the database with initial users for development/testing.
/// Only run in Development.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        // Ensure schema is up to date (applies migrations)
        await db.Database.MigrateAsync(ct);

        // If we already have users, do nothing (idempotent seed).
        if (await db.Users.AnyAsync(ct))
            return;

        // Create deterministic demo users (for teacher demos and tests)
        // NOTE: Passwords are simple on purpose (demo). We'll improve later.
        var users = new List<User>
        {
            CreateUser("subscriber", "subscriber123", UserRole.Subscriber),
            CreateUser("writer", "writer123", UserRole.Writer),
            CreateUser("editor", "editor123", UserRole.Editor),
            CreateUser("investigator", "investigator123", UserRole.Investigator)
        };

        await db.Users.AddRangeAsync(users, ct);
        await db.SaveChangesAsync(ct);
    }

    private static User CreateUser(string username, string plainPassword, UserRole role)
    {
        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.HashPassword(plainPassword, salt);

        // Store salt + hash together (simple format for demo)
        // In production: separate columns or use Identity.
        var passwordHash = $"{salt}${hash}";

        return new User(username, passwordHash, role);
    }
}
