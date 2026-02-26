namespace WhistleblowerNews.Domain;

public enum AuditEventType
{
    LoginSucceeded = 0,
    LoginFailed = 1,
    ReportStatusChanged = 2,
    ReportInfoRequested = 3,
    CommentDeleted = 4,
    CommentUpdated = 5,
    ArticleUpdated = 6,
    ArticleDeleted = 7,
    AuthorizationDenied = 8,
    RateLimitTriggered = 9
}
