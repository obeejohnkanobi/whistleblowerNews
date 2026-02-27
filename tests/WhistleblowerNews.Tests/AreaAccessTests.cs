using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WhistleblowerNews.Application.Authentication;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Tests;

public sealed class AreaAccessTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AreaAccessTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/Public/Articles")]
    [InlineData("/Whistleblower/Reports/Create")]
    [InlineData("/Whistleblower/Reports/Track")]
    public async Task PublicAreas_AllowAnonymous(string path)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Writer/Articles")]
    [InlineData("/Writer/Articles/Create")]
    public async Task WriterAreas_RequireWriter(string path)
    {
        var writerClient = await CreateAuthenticatedClient(UserRole.Writer);

        var response = await writerClient.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WriterAreas_DenySubscriber()
    {
        var subscriberClient = await CreateAuthenticatedClient(UserRole.Subscriber);

        var response = await subscriberClient.GetAsync("/Writer/Articles");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/Account/AccessDenied", response.Headers.Location!.PathAndQuery, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubscriberArea_RequiresSubscriber()
    {
        var subscriberClient = await CreateAuthenticatedClient(UserRole.Subscriber);

        var response = await subscriberClient.GetAsync("/Subscriber/Comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InvestigatorArea_RequiresInvestigator()
    {
        var investigatorClient = await CreateAuthenticatedClient(UserRole.Investigator);

        var response = await investigatorClient.GetAsync("/Investigator/Reports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InvestigatorArea_DenySubscriber()
    {
        var subscriberClient = await CreateAuthenticatedClient(UserRole.Subscriber);

        var response = await subscriberClient.GetAsync("/Investigator/Reports");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/Account/AccessDenied", response.Headers.Location!.PathAndQuery, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/Editor/Articles")]
    [InlineData("/Editor/Comments")]
    public async Task EditorAreas_RequireEditor(string path)
    {
        var editorClient = await CreateAuthenticatedClient(UserRole.Editor);

        var response = await editorClient.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EditorAreas_DenyWriter()
    {
        var writerClient = await CreateAuthenticatedClient(UserRole.Writer);

        var response = await writerClient.GetAsync("/Editor/Articles");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/Account/AccessDenied", response.Headers.Location!.PathAndQuery, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<HttpClient> CreateAuthenticatedClient(UserRole role)
    {
        var password = "password1";
        var user = await TestData.CreateUserAsync(_factory, role, null, password);
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(user.UserName ?? string.Empty, password));

        response.EnsureSuccessStatusCode();
        return client;
    }
}
