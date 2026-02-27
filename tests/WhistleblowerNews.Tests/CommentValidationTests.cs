using System.Net;
using System.Net.Http.Json;
using WhistleblowerNews.Application.Comments;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Tests;

public sealed class CommentValidationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CommentValidationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateComment_EmptyContent_ReturnsBadRequest()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var (_, subscriberClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);

        var response = await subscriberClient.PostAsJsonAsync(
            $"/api/articles/{article.Id}/comments",
            new CreateCommentRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateComment_NonExistentArticle_ReturnsNotFound()
    {
        var (_, subscriberClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);

        var response = await subscriberClient.PostAsJsonAsync(
            "/api/articles/999999/comments",
            new CreateCommentRequest("Nice article"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateComment_EmptyContent_ReturnsBadRequest()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriber, subscriberClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);

        var response = await subscriberClient.PutAsJsonAsync(
            $"/api/comments/{comment.Id}",
            new UpdateCommentRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateComment_NonExistent_ReturnsNotFound()
    {
        var (_, subscriberClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);

        var response = await subscriberClient.PutAsJsonAsync(
            "/api/comments/999999",
            new UpdateCommentRequest("Updated content"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_NonExistent_ReturnsNotFound()
    {
        var (_, subscriberClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);

        var response = await subscriberClient.DeleteAsync("/api/comments/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
