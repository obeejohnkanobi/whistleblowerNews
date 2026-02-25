namespace whistleblowerNews.Domain;

/// <summary>
/// Message on a report (reporter or investigator).
/// </summary>
public class ReportMessage
{
    public int Id { get; private set; }

    public Guid CaseId { get; private set; }
    public Report Report { get; private set; } = null!;

    public ReportSenderType SenderType { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    private ReportMessage() { }

    public ReportMessage(Guid caseId, ReportSenderType senderType, string content)
    {
        CaseId = caseId;
        SenderType = senderType;
        Content = content;
        CreatedAt = DateTime.UtcNow;
    }
}