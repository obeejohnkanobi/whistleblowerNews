using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using WhistleblowerNews.Application.Authentication;
using WhistleblowerNews.Domain;
using WhistleblowerNews.Infrastructure;

namespace WhistleblowerNews.Tests;

public sealed class AuthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_MissingUsername_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("", "password"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingPassword_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("user", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidUser_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("missing", "password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var username = UniqueUsername("writer");
        await TestData.CreateUserAsync(_factory, UserRole.Writer, username, "correct-password1");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidUser_IsAudited()
    {
        var username = UniqueUsername("missing");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, "password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = db.AuditLogEntries.FirstOrDefault(a =>
            a.EventType == AuditEventType.LoginFailed &&
            a.TargetId == username);

        Assert.NotNull(audit);
        Assert.Equal(AuditOutcome.Failed, audit!.Outcome);
    }

    [Fact]
    public async Task Login_ValidUser_SetsCookie()
    {
        var username = UniqueUsername("writer");
        await TestData.CreateUserAsync(_factory, UserRole.Writer, username, "writer-password1");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, "writer-password1"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Me_WithValidLogin_ReturnsUserInfo()
    {
        var username = UniqueUsername("editor");
        await TestData.CreateUserAsync(_factory, UserRole.Editor, username, "editor-password1");

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, "editor-password1"));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(username, doc.RootElement.GetProperty("username").GetString());
        Assert.Equal("Editor", doc.RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_LockedOutUser_Returns423()
    {
        var username = UniqueUsername("writer");
        var user = await TestData.CreateUserAsync(_factory, UserRole.Writer, username, "locked-password1");
        await TestData.LockoutUserAsync(_factory, user);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, "locked-password1"));

        Assert.Equal(HttpStatusCode.Locked, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnconfirmedEmail_ReturnsForbidden()
    {
        var username = UniqueUsername("subscriber");
        var user = await TestData.CreateUnconfirmedUserAsync(_factory, UserRole.Subscriber, username, "unconfirmed-password1");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, "unconfirmed-password1"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Logout_Succeeds()
    {
        var username = UniqueUsername("writer");
        await TestData.CreateUserAsync(_factory, UserRole.Writer, username, "writer-password1");

        await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, "writer-password1"));

        var response = await _client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidUser_IsAudited()
    {
        var username = UniqueUsername("writer");
        var user = await TestData.CreateUserAsync(_factory, UserRole.Writer, username, "audit-password1");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, "audit-password1"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = db.AuditLogEntries.FirstOrDefault(a =>
            a.EventType == AuditEventType.LoginSucceeded &&
            a.TargetId == user.Id.ToString());

        Assert.NotNull(audit);
        Assert.Equal(AuditOutcome.Success, audit!.Outcome);
    }

    private static string UniqueUsername(string prefix) =>
        $"{prefix}_{Guid.NewGuid():N}";
}
