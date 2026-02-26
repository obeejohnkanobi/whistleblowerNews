using System.Security.Cryptography;
using System.Text;

namespace WhistleblowerNews.Application.Services;

/// <summary>
/// Simple password hashing for demo purposes.
/// Uses SHA-256 with a per-user salt.
/// In production, prefer ASP.NET Core Identity (PBKDF2/BCrypt/Argon2).
/// </summary>
public static class PasswordHasher
{
    public static string HashPassword(string password, string salt)
    {
        var bytes = Encoding.UTF8.GetBytes($"{salt}:{password}");
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public static string GenerateSalt(int bytes = 16)
    {
        var buffer = new byte[bytes];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToHexString(buffer);
    }

    public static bool VerifyHash(string password, string salt, string expectedHash)
    {
        var actualHash = HashPassword(password, salt);

        try
        {
            var expectedBytes = Convert.FromHexString(expectedHash);
            var actualBytes = Convert.FromHexString(actualHash);
            return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

