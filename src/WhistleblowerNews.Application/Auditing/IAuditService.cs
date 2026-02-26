using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Application.Auditing;

public sealed record AuditContext(
    int? ActorUserId,
    string? ActorRole,
    string? CorrelationId,
    string? IpAddress,
    string? UserAgent);

public sealed record AuditEntry(
    AuditEventType EventType,
    AuditOutcome Outcome,
    string TargetType,
    string TargetId,
    Guid? CaseId,
    AuditContext Context);

public interface IAuditService
{
    Task Success(
        AuditEventType eventType,
        string targetType,
        string targetId,
        Guid? caseId,
        AuditContext context,
        CancellationToken ct = default);

    Task Denied(
        AuditEventType eventType,
        string targetType,
        string targetId,
        Guid? caseId,
        AuditContext context,
        CancellationToken ct = default);

    Task Failed(
        AuditEventType eventType,
        string targetType,
        string targetId,
        Guid? caseId,
        AuditContext context,
        CancellationToken ct = default);
}
