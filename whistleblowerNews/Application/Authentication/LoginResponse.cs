namespace whistleblowerNews.Application.Auth;

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);