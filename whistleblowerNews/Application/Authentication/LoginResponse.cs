namespace whistleblowerNews.Application.Authentication;

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);