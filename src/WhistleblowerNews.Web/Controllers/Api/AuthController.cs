using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Application.Auditing;
using WhistleblowerNews.Application.Authentication;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Domain;
using WhistleblowerNews.Web.Infrastructure;

namespace WhistleblowerNews.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService auth, IAuditService audit, ILogger<AuthController> logger)
    {
        _auth = auth;
        _audit = audit;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.ValidateCredentialsAsync(request.Username, request.Password, ct);
        if (result.Status == ResultStatus.BadRequest)
            return BadRequest(result.Error);

        if (result.Status == ResultStatus.Unauthorized)
        {
            _logger.LogWarning("Login failed for {Username}", request.Username);
            var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
            var targetId = string.IsNullOrWhiteSpace(request.Username) ? "unknown" : request.Username;
            await _audit.Failed(
                AuditEventType.LoginFailed,
                "User",
                targetId,
                null,
                auditContext,
                ct);

            return Unauthorized();
        }

        var user = result.Value!;
        await SignInAsync(user);

        _logger.LogInformation("Login succeeded for {UserId}", user.Id);
        var successContext = AuditContextFactory.FromHttpContext(HttpContext) with
        {
            ActorUserId = user.Id,
            ActorRole = user.Role.ToString()
        };
        await _audit.Success(
            AuditEventType.LoginSucceeded,
            "User",
            user.Id.ToString(),
            null,
            successContext,
            ct);

        return Ok(new { username = user.Username, role = user.Role.ToString() });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            Username = User.Identity?.Name,
            Role = User.FindFirst(ClaimTypes.Role)?.Value
        });
    }

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false
            });
    }
}
