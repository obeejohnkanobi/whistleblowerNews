using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WhistleblowerNews.Application.Authentication;
using WhistleblowerNews.Application.Articles;
using WhistleblowerNews.Application.Comments;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Tests;

public sealed class UiRegressionTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UiRegressionTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_AreaAccess_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Writer/Articles");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/Account/Login", response.Headers.Location!.PathAndQuery, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Subscriber_CannotAccess_EditorArea()
    {
        var subscriberClient = await CreateAuthenticatedClient(UserRole.Subscriber);
        var response = await subscriberClient.GetAsync("/Editor/Articles");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/Account/AccessDenied", response.Headers.Location!.PathAndQuery, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Editor_CanAccess_EditorArea()
    {
        var editorClient = await CreateAuthenticatedClient(UserRole.Editor, allowRedirect: true);
        var response = await editorClient.GetAsync("/Editor/Articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Writer_CanAccess_WriterArea()
    {
        var writerClient = await CreateAuthenticatedClient(UserRole.Writer, allowRedirect: true);
        var response = await writerClient.GetAsync("/Writer/Articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Antiforgery_MissingToken_ReturnsBadRequest()
    {
        var writerClient = await CreateAuthenticatedClient(UserRole.Writer);

        var response = await writerClient.PostAsync(
            "/Account/Logout",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Antiforgery_MissingToken_OnCommentDelete_ReturnsBadRequest()
    {
        var writer = await TestData.CreateUserAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var subscriber = await TestData.CreateUserAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);

        var subscriberClient = await CreateAuthenticatedClient(subscriber);
        var response = await subscriberClient.PostAsync(
            $"/Subscriber/Comments/Delete/{comment.Id}",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClient(UserRole role, bool allowRedirect = false)
    {
        var password = "password1";
        var user = await TestData.CreateUserAsync(_factory, role, null, password);
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = allowRedirect });

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(user.UserName ?? string.Empty, password));

        response.EnsureSuccessStatusCode();
        return client;
    }

    private async Task<HttpClient> CreateAuthenticatedClient(User user, bool allowRedirect = false)
    {
        var password = "password1";
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = allowRedirect });

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(user.UserName ?? string.Empty, password));

        response.EnsureSuccessStatusCode();
        return client;
    }
}
