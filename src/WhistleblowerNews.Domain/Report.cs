namespace WhistleblowerNews.Domain;

/// <summary>
/// Represents a whistleblower report.
/// </summary>
public class Report
{
    public Guid CaseId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public ReportStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public ICollection<ReportMessage> Messages { get; private set; } = new List<ReportMessage>();

    public ICollection<InvestigatorAssignment> Assignments { get; private set; } = new List<InvestigatorAssignment>();

    public ICollection<AuditLogEntry> AuditLogs { get; private set; } = new List<AuditLogEntry>();

    public ReporterSecret ReporterSecret { get; private set; } = null!;

    private Report() { }

    public Report(Guid caseId, string title, string description)
    {
        CaseId = caseId;
        Title = title;
        Description = description;
        Status = ReportStatus.Open;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(ReportStatus status)
    {
        Status = status;
    }

    public void AddMessage(ReportMessage message)
    {
        Messages.Add(message);
    }

    public void AddAudit(AuditLogEntry audit)
    {
        AuditLogs.Add(audit);
    }
}
