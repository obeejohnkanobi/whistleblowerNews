namespace WhistleblowerNews.Application.Authentication;

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);
