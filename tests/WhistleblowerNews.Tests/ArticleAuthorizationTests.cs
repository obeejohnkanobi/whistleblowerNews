using System.Net;
using System.Net.Http.Json;
using WhistleblowerNews.Application.Articles;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Tests;

public sealed class ArticleAuthorizationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ArticleAuthorizationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetArticles_AllowsGuest()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        await TestData.CreateArticleAsync(_factory, writer.Id);

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateArticle_RequiresWriter()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/articles",
            new CreateArticleRequest("Title", "Content"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Writer_CanCreateArticle()
    {
        var (_, client) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);

        var response = await client.PostAsJsonAsync(
            "/api/articles",
            new CreateArticleRequest("Title", "Content"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Writer_CannotEditOthersArticle()
    {
        var (writerA, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var (_, clientB) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writerA.Id);

        var response = await clientB.PutAsJsonAsync(
            $"/api/articles/{article.Id}",
            new UpdateArticleRequest("New Title", "New Content"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Editor_CanEditAnyArticle()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var (_, editorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Editor);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);

        var response = await editorClient.PutAsJsonAsync(
            $"/api/articles/{article.Id}",
            new UpdateArticleRequest("New Title", "New Content"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Writer_CanDeleteOwnArticle()
    {
        var (writer, client) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);

        var response = await client.DeleteAsync($"/api/articles/{article.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Editor_CanDeleteAnyArticle()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var (_, editorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Editor);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);

        var response = await editorClient.DeleteAsync($"/api/articles/{article.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
