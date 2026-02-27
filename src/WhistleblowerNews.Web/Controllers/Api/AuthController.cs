using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WhistleblowerNews.Application.Auditing;
using WhistleblowerNews.Application.Authentication;
using WhistleblowerNews.Domain;
using WhistleblowerNews.Web.Infrastructure;

namespace WhistleblowerNews.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IAuditService audit,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _audit = audit;
        _logger = logger;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-login-policy")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Username and password are required.");

        var signInResult = await _signInManager.PasswordSignInAsync(
            request.Username,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (signInResult.Succeeded)
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user is null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            _logger.LogInformation("Login succeeded for {UserId}", user.Id);
            var successContext = AuditContextFactory.FromHttpContext(HttpContext) with
            {
                ActorUserId = user.Id,
                ActorRole = role
            };
            await _audit.Success(
                AuditEventType.LoginSucceeded,
                "User",
                user.Id.ToString(),
                null,
                successContext,
                ct);

            return Ok(new { username = user.UserName, role });
        }

        if (signInResult.IsLockedOut)
        {
            _logger.LogWarning("Login locked out for {Username}", request.Username);
            return StatusCode(StatusCodes.Status423Locked);
        }

        if (signInResult.IsNotAllowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Email not confirmed.");
        }

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

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
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
}
