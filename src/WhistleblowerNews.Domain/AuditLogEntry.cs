namespace WhistleblowerNews.Domain;

/// <summary>
/// Audit log entry for security-relevant actions.
/// Append-only.
/// </summary>
public class AuditLogEntry
{
    public int Id { get; private set; }

    public Guid? CaseId { get; private set; }
    public Report? Report { get; private set; }

    public int? ActorUserId { get; private set; }
    public User? Actor { get; private set; }

    public string? ActorRole { get; private set; }

    public AuditEventType EventType { get; private set; }

    public string TargetType { get; private set; } = string.Empty;

    public string TargetId { get; private set; } = string.Empty;

    public AuditOutcome Outcome { get; private set; }

    public string? CorrelationId { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private AuditLogEntry() { }

    public AuditLogEntry(
        AuditEventType eventType,
        AuditOutcome outcome,
        string targetType,
        string targetId,
        Guid? caseId,
        int? actorUserId,
        string? actorRole,
        string? correlationId,
        string? ipAddress,
        string? userAgent)
    {
        EventType = eventType;
        Outcome = outcome;
        TargetType = targetType;
        TargetId = targetId;
        CaseId = caseId;
        ActorUserId = actorUserId;
        ActorRole = actorRole;
        CorrelationId = correlationId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CreatedAt = DateTime.UtcNow;
    }
}
