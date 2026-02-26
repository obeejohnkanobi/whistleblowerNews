namespace WhistleblowerNews.Domain;

/// <summary>
/// Defines the roles available in the system.
/// Following principle of least privilege.
/// </summary>
public enum UserRole
{
    Subscriber = 0,
    Writer = 1,
    Editor = 2,
    Investigator = 3
}

