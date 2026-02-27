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

/// <summary>
/// Canonical role name strings used by ASP.NET Core Identity.
/// Prefer these constants over <c>UserRole.X.ToString()</c> to avoid
/// silent breakage if enum member names are ever renamed.
/// </summary>
public static class UserRoles
{
    public const string Subscriber  = "Subscriber";
    public const string Writer      = "Writer";
    public const string Editor      = "Editor";
    public const string Investigator = "Investigator";

    /// <summary>All role names in least-privilege order.</summary>
    public static readonly IReadOnlyList<string> All =
        [Subscriber, Writer, Editor, Investigator];
}

/// <summary>Extension methods for <see cref="UserRole"/>.</summary>
public static class UserRoleExtensions
{
    /// <summary>
    /// Returns the canonical Identity role name string for this role.
    /// </summary>
    public static string ToRoleName(this UserRole role) => role switch
    {
        UserRole.Subscriber  => UserRoles.Subscriber,
        UserRole.Writer      => UserRoles.Writer,
        UserRole.Editor      => UserRoles.Editor,
        UserRole.Investigator => UserRoles.Investigator,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };
}

