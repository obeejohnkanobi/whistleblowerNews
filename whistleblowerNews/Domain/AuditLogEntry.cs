namespace whistleblowerNews.Domain;

/// <summary>
/// Audit log entry for report actions.
/// </summary>
public class AuditLogEntry
{
    public int Id { get; private set; }

    public Guid CaseId { get; private set; }
    public Report Report { get; private set; } = null!;

    public int ActorUserId { get; private set; }
    public User Actor { get; private set; } = null!;

    public string Action { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    private AuditLogEntry() { }

    public AuditLogEntry(Guid caseId, int actorUserId, string action)
    {
        CaseId = caseId;
        ActorUserId = actorUserId;
        Action = action;
        CreatedAt = DateTime.UtcNow;
    }
}