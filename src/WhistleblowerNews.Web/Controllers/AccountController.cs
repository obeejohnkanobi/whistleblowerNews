using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Application.Auditing;
using WhistleblowerNews.Domain;
using WhistleblowerNews.Web.Infrastructure;
using WhistleblowerNews.Web.Infrastructure.Email;
using WhistleblowerNews.Web.Models.Account;

namespace WhistleblowerNews.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly IAuditService _audit;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IEmailSender emailSender,
        IAuditService audit,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
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
    [EnableRateLimiting("auth-login-policy")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var signInResult = await _signInManager.PasswordSignInAsync(
            model.Username,
            model.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (signInResult.Succeeded)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user is not null)
            {
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
            }

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        if (signInResult.IsLockedOut)
        {
            _logger.LogWarning("Login locked out for {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "Account locked due to repeated failed attempts. Try again later.");
            return View(model);
        }

        if (signInResult.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "Email not confirmed. Check your email for the confirmation link.");
            return View(model);
        }

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

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth-register-policy")]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new User
        {
            UserName = model.Username.Trim(),
            Email = model.Email.Trim(),
            LockoutEnabled = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        await _userManager.AddToRoleAsync(user, UserRole.Subscriber.ToString());

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebUtility.UrlEncode(token);
        var callbackUrl = Url.Action(
            "ConfirmEmail",
            "Account",
            new { userId = user.Id, token = encodedToken },
            Request.Scheme);

        if (!string.IsNullOrWhiteSpace(callbackUrl))
        {
            var body = $"""
                Please confirm your email address by clicking the link below:
                {callbackUrl}
                """;
            await _emailSender.SendAsync(user.Email!, "Confirm your WhistleblowerNews account", body, ct);
        }

        _logger.LogInformation("Subscriber registered {UserId}", user.Id);
        var auditContext = AuditContextFactory.FromHttpContext(HttpContext) with
        {
            ActorUserId = user.Id,
            ActorRole = UserRole.Subscriber.ToString()
        };
        await _audit.Success(
            AuditEventType.SubscriberRegistered,
            "User",
            user.Id.ToString(),
            null,
            auditContext,
            ct);

        ViewData["Email"] = user.Email;
        return View("RegisterConfirmation");
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(int userId, string token)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(token))
        {
            ViewData["StatusMessage"] = "Invalid confirmation link.";
            return View();
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            ViewData["StatusMessage"] = "Invalid confirmation link.";
            return View();
        }

        var decodedToken = WebUtility.UrlDecode(token);
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        ViewData["StatusMessage"] = result.Succeeded
            ? "Your email has been confirmed. You can now sign in."
            : "Unable to confirm your email.";

        return View();
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null || !await _userManager.IsEmailConfirmedAsync(user))
            return View("ForgotPasswordConfirmation");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebUtility.UrlEncode(token);
        var callbackUrl = Url.Action(
            "ResetPassword",
            "Account",
            new { token = encodedToken, email = model.Email.Trim() },
            Request.Scheme);

        if (!string.IsNullOrWhiteSpace(callbackUrl))
        {
            var body = $"""
                Reset your password using the link below:
                {callbackUrl}
                """;
            await _emailSender.SendAsync(user.Email!, "Reset your WhistleblowerNews password", body, ct);
        }

        return View("ForgotPasswordConfirmation");
    }

    [HttpGet]
    public IActionResult ResetPassword(string token, string email)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            return View("ResetPasswordConfirmation");

        return View(new ResetPasswordViewModel
        {
            Token = token,
            Email = email
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
            return View("ResetPasswordConfirmation");

        var decodedToken = WebUtility.UrlDecode(model.Token);
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        return View("ResetPasswordConfirmation");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
