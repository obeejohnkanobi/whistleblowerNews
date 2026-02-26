using WhistleblowerNews.Application.Auditing;
using WhistleblowerNews.Domain;
using WhistleblowerNews.Infrastructure;

namespace WhistleblowerNews.Web.Infrastructure;

public sealed class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task Success(
        AuditEventType eventType,
        string targetType,
        string targetId,
        Guid? caseId,
        AuditContext context,
        CancellationToken ct = default)
    {
        return RecordAsync(new AuditEntry(
            eventType,
            AuditOutcome.Success,
            targetType,
            targetId,
            caseId,
            context), ct);
    }

    public Task Denied(
        AuditEventType eventType,
        string targetType,
        string targetId,
        Guid? caseId,
        AuditContext context,
        CancellationToken ct = default)
    {
        return RecordAsync(new AuditEntry(
            eventType,
            AuditOutcome.Denied,
            targetType,
            targetId,
            caseId,
            context), ct);
    }

    public Task Failed(
        AuditEventType eventType,
        string targetType,
        string targetId,
        Guid? caseId,
        AuditContext context,
        CancellationToken ct = default)
    {
        return RecordAsync(new AuditEntry(
            eventType,
            AuditOutcome.Failed,
            targetType,
            targetId,
            caseId,
            context), ct);
    }

    private async Task RecordAsync(AuditEntry entry, CancellationToken ct = default)
    {
        var audit = new AuditLogEntry(
            entry.EventType,
            entry.Outcome,
            entry.TargetType,
            entry.TargetId,
            entry.CaseId,
            entry.Context.ActorUserId,
            entry.Context.ActorRole,
            entry.Context.CorrelationId,
            entry.Context.IpAddress,
            Truncate(entry.Context.UserAgent, 256));

        _db.AuditLogEntries.Add(audit);
        await _db.SaveChangesAsync(ct);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
