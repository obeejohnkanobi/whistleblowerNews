using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using whistleblowerNews.Application.Authentication;
using whistleblowerNews.Infrastructure;
using whistleblowerNews.Services;

namespace whistleblowerNews.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JwtTokenService _jwt;

    public AuthController(ApplicationDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    // POST /auth/login
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required.");
        }

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null)
            return Unauthorized("Invalid credentials.");

        // PasswordHash format = "salt$hash"
        var parts = user.PasswordHash.Split('$');
        if (parts.Length != 2)
            return StatusCode(500, "Password hash format is invalid.");

        var salt = parts[0];
        var expectedHash = parts[1];

        if (!PasswordHasher.VerifyHash(request.Password, salt, expectedHash))
            return Unauthorized("Invalid credentials.");

        var (token, expiresAtUtc) = _jwt.CreateToken(user);

        return Ok(new LoginResponse(token, expiresAtUtc));
    }

    // GET /auth/me (requires JWT)
    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> Me()
    {
        return Ok(new
        {
            Username = User.Identity?.Name, // may be null unless we map Name claim (see note below)
            Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
        });
    }

}
