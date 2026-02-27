using System.Net;
using System.Net.Http.Json;
using WhistleblowerNews.Application.Articles;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Tests;

public sealed class ArticleValidationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ArticleValidationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateArticle_EmptyTitle_ReturnsBadRequest()
    {
        var (_, writerClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);

        var response = await writerClient.PostAsJsonAsync(
            "/api/articles",
            new CreateArticleRequest("", "Some content"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateArticle_EmptyContent_ReturnsBadRequest()
    {
        var (_, writerClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);

        var response = await writerClient.PostAsJsonAsync(
            "/api/articles",
            new CreateArticleRequest("Some title", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateArticle_EmptyTitle_ReturnsBadRequest()
    {
        var (writer, writerClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);

        var response = await writerClient.PutAsJsonAsync(
            $"/api/articles/{article.Id}",
            new UpdateArticleRequest("", "Updated content"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateArticle_EmptyContent_ReturnsBadRequest()
    {
        var (writer, writerClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);

        var response = await writerClient.PutAsJsonAsync(
            $"/api/articles/{article.Id}",
            new UpdateArticleRequest("Updated title", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateArticle_NonExistent_ReturnsNotFound()
    {
        var (_, writerClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);

        var response = await writerClient.PutAsJsonAsync(
            "/api/articles/999999",
            new UpdateArticleRequest("Title", "Content"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteArticle_NonExistent_ReturnsNotFound()
    {
        var (_, writerClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);

        var response = await writerClient.DeleteAsync("/api/articles/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
