using System.Net;
using System.Net.Http.Json;
using WhistleblowerNews.Application.Comments;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Tests;

public sealed class CommentAuthorizationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CommentAuthorizationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetComments_AllowsGuest()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriber, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/articles/{article.Id}/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Subscriber_CanCreateComment()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (_, subscriberClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);

        var response = await subscriberClient.PostAsJsonAsync(
            $"/api/articles/{article.Id}/comments",
            new CreateCommentRequest("Nice article"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateComment_RequiresSubscriber()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/api/articles/{article.Id}/comments",
            new CreateCommentRequest("Nice article"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Subscriber_CannotEditOthersComment()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriberA, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        var (_, subscriberClientB) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriberA.Id);

        var response = await subscriberClientB.PutAsJsonAsync(
            $"/api/comments/{comment.Id}",
            new UpdateCommentRequest("Updated"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Editor_CanEditAnyComment()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriber, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);
        var (_, editorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Editor);

        var response = await editorClient.PutAsJsonAsync(
            $"/api/comments/{comment.Id}",
            new UpdateCommentRequest("Updated"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Subscriber_CanDeleteOwnComment()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriber, subscriberClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);

        var response = await subscriberClient.DeleteAsync($"/api/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Editor_CanDeleteAnyComment()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriber, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);
        var (_, editorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Editor);

        var response = await editorClient.DeleteAsync($"/api/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Writer_CanDeleteCommentsOnOwnArticle()
    {
        var (writer, writerClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriber, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);

        var response = await writerClient.DeleteAsync($"/api/articles/{article.Id}/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Writer_CannotDeleteCommentsOnOtherArticles()
    {
        var (writerA, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var (_, writerClientB) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writerA.Id);
        var (subscriber, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);

        var response = await writerClientB.DeleteAsync($"/api/articles/{article.Id}/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
