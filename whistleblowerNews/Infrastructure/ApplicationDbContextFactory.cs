using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace whistleblowerNews.Infrastructure;

/// <summary>
/// Design-time factory used by EF Core tools (dotnet ef) to create ApplicationDbContext
/// when the app isn't running. This avoids DI issues during migrations.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Use the same connection string as appsettings.json (simple and deterministic)
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // NOTE: This is only used for migrations, not runtime DI.
        optionsBuilder.UseSqlite("Data Source=whistleblowerNews.db");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}