using Microsoft.Extensions.Diagnostics.HealthChecks;
using WhistleblowerNews.Infrastructure;

namespace WhistleblowerNews.Web.Infrastructure;

public sealed class DbHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _db;

    public DbHealthCheck(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database connection OK")
                : HealthCheckResult.Unhealthy("Database connection failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}
