namespace WhistleblowerNews.Domain;

/// <summary>
/// Stores the hashed reporter secret for a case.
/// </summary>
public class ReporterSecret
{
    public Guid CaseId { get; private set; }
    public Report Report { get; private set; } = null!;

    public string SecretHash { get; private set; } = string.Empty;

    private ReporterSecret() { }

    public ReporterSecret(Guid caseId, string secretHash)
    {
        CaseId = caseId;
        SecretHash = secretHash;
    }
}
