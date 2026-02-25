namespace whistleblowerNews.Authorization;

public static class AuthorizationPolicies
{
    public const string IsWriter = "IsWriter";
    public const string IsEditor = "IsEditor";
    public const string IsSubscriber = "IsSubscriber";
    public const string IsInvestigator = "IsInvestigator";

    public const string ArticleOwnerOrEditor = "ArticleOwnerOrEditor";
    public const string CommentOwnerOrEditor = "CommentOwnerOrEditor";
    public const string WriterOwnsArticle = "WriterOwnsArticle";
}