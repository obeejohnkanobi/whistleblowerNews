namespace WhistleblowerNews.Domain;

/// <summary>
/// Stores the hashed reporter secret for a case.
/// </summary>
public class ReporterSecret
{
    public Guid CaseId { get; private set; }
    public Report Report { get; private set; } = null!;

    public string SecretHash { get; private set; } = string.Empty;

    /// <summary>When the token was issued. Used to enforce expiry.</summary>
    public DateTime CreatedAt { get; private set; }

    private ReporterSecret() { }

    public ReporterSecret(Guid caseId, string secretHash)
    {
        CaseId = caseId;
        SecretHash = secretHash;
        CreatedAt = DateTime.UtcNow;
    }

    public void RotateSecret(string newSecretHash)
    {
        SecretHash = newSecretHash;
        CreatedAt = DateTime.UtcNow;
    }
}
