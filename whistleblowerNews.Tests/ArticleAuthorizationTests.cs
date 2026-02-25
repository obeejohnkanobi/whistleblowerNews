using System.Net;
using System.Net.Http.Json;
using whistleblowerNews.Application.Articles;
using whistleblowerNews.Domain;

namespace whistleblowerNews.Tests;

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
        var (writer, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        await TestData.CreateArticleAsync(_factory, writer.Id);

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateArticle_RequiresWriter()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/articles",
            new CreateArticleRequest("Title", "Content"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Writer_CanCreateArticle()
    {
        var (_, token) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var client = TestData.CreateAuthenticatedClient(_factory, token);

        var response = await client.PostAsJsonAsync(
            "/articles",
            new CreateArticleRequest("Title", "Content"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Writer_CannotEditOthersArticle()
    {
        var (writerA, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var (writerB, tokenB) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writerA.Id);

        var client = TestData.CreateAuthenticatedClient(_factory, tokenB);
        var response = await client.PutAsJsonAsync(
            $"/articles/{article.Id}",
            new UpdateArticleRequest("New Title", "New Content"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Editor_CanEditAnyArticle()
    {
        var (writer, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var (_, editorToken) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Editor);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);

        var client = TestData.CreateAuthenticatedClient(_factory, editorToken);
        var response = await client.PutAsJsonAsync(
            $"/articles/{article.Id}",
            new UpdateArticleRequest("New Title", "New Content"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Writer_CanDeleteOwnArticle()
    {
        var (writer, token) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);

        var client = TestData.CreateAuthenticatedClient(_factory, token);
        var response = await client.DeleteAsync($"/articles/{article.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Editor_CanDeleteAnyArticle()
    {
        var (writer, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var (_, editorToken) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Editor);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);

        var client = TestData.CreateAuthenticatedClient(_factory, editorToken);
        var response = await client.DeleteAsync($"/articles/{article.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}