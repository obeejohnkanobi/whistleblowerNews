using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using whistleblowerNews.Application.Authentication;
using whistleblowerNews.Domain;
using whistleblowerNews.Infrastructure;
using whistleblowerNews.Services;

namespace whistleblowerNews.Tests;

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
            "/auth/login",
            new LoginRequest("", "password"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingPassword_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("user", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidUser_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("missing", "password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var username = UniqueUsername("writer");
        await SeedUserAsync(username, "correct-password", UserRole.Writer);

        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(username, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidUser_ReturnsToken()
    {
        var username = UniqueUsername("writer");
        await SeedUserAsync(username, "writer-password", UserRole.Writer);

        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(username, "writer-password"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
        Assert.True(payload.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsUserInfo()
    {
        var username = UniqueUsername("editor");
        await SeedUserAsync(username, "editor-password", UserRole.Editor);

        var loginResponse = await _client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(username, "editor-password"));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginPayload);

        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", loginPayload!.AccessToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(username, doc.RootElement.GetProperty("username").GetString());
        Assert.Equal("Editor", doc.RootElement.GetProperty("role").GetString());
    }

    private async Task SeedUserAsync(string username, string password, UserRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var salt = "TEST_SALT";
        var hash = PasswordHasher.HashPassword(password, salt);
        var user = new User(username, $"{salt}${hash}", role);

        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private static string UniqueUsername(string prefix) =>
        $"{prefix}_{Guid.NewGuid():N}";
}