using Microsoft.EntityFrameworkCore;
using WhistleblowerNews.Application.Abstractions;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Application.Services;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Application.Authentication;

public sealed class AuthService
{
    private readonly IApplicationDbContext _db;

    public AuthService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<User>> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return ServiceResult<User>.BadRequest("Username and password are required.");

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username, ct);
        if (user is null)
            return ServiceResult<User>.Unauthorized("Invalid credentials.");

        var parts = user.PasswordHash.Split('$');
        if (parts.Length != 2)
            return ServiceResult<User>.Unauthorized("Invalid credentials.");

        var salt = parts[0];
        var expectedHash = parts[1];

        if (!PasswordHasher.VerifyHash(password, salt, expectedHash))
            return ServiceResult<User>.Unauthorized("Invalid credentials.");

        return ServiceResult<User>.Ok(user);
    }
}
