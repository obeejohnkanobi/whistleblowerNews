using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Application.Auditing;
using WhistleblowerNews.Application.Authentication;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Domain;
using WhistleblowerNews.Web.Infrastructure;
using WhistleblowerNews.Web.Models.Account;

namespace WhistleblowerNews.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly AuthService _auth;
    private readonly IAuditService _audit;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AuthService auth, IAuditService audit, ILogger<AccountController> logger)
    {
        _auth = auth;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _auth.ValidateCredentialsAsync(model.Username, model.Password, ct);
        if (result.Status == ResultStatus.BadRequest)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Invalid credentials.");
            return View(model);
        }

        if (result.Status == ResultStatus.Unauthorized)
        {
            _logger.LogWarning("Login failed for {Username}", model.Username);
            var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
            var targetId = string.IsNullOrWhiteSpace(model.Username) ? "unknown" : model.Username;
            await _audit.Failed(
                AuditEventType.LoginFailed,
                "User",
                targetId,
                null,
                auditContext,
                ct);

            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(model);
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

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
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
            new AuthenticationProperties { IsPersistent = false });
    }
}
