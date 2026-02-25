namespace whistleblowerNews.Domain;

/// <summary>
/// Assigns an investigator to a report.
/// </summary>
public class InvestigatorAssignment
{
    public int Id { get; private set; }

    public Guid CaseId { get; private set; }
    public Report Report { get; private set; } = null!;

    public int InvestigatorUserId { get; private set; }
    public User Investigator { get; private set; } = null!;

    public DateTime AssignedAt { get; private set; }

    private InvestigatorAssignment() { }

    public InvestigatorAssignment(Guid caseId, int investigatorUserId)
    {
        CaseId = caseId;
        InvestigatorUserId = investigatorUserId;
        AssignedAt = DateTime.UtcNow;
    }
}