using System.Net;
using System.Net.Http.Json;
using whistleblowerNews.Application.Comments;
using whistleblowerNews.Domain;

namespace whistleblowerNews.Tests;

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
        var (writer, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriber, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Subscriber);
        await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/articles/{article.Id}/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Subscriber_CanCreateComment()
    {
        var (writer, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (_, token) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Subscriber);

        var client = TestData.CreateAuthenticatedClient(_factory, token);
        var response = await client.PostAsJsonAsync(
            $"/articles/{article.Id}/comments",
            new CreateCommentRequest("Nice article"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateComment_RequiresSubscriber()
    {
        var (writer, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/articles/{article.Id}/comments",
            new CreateCommentRequest("Nice article"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Subscriber_CannotEditOthersComment()
    {
        var (writer, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriberA, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Subscriber);
        var (subscriberB, tokenB) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriberA.Id);

        var client = TestData.CreateAuthenticatedClient(_factory, tokenB);
        var response = await client.PutAsJsonAsync(
            $"/comments/{comment.Id}",
            new UpdateCommentRequest("Updated"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Editor_CanEditAnyComment()
    {
        var (writer, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriber, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);
        var (_, editorToken) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Editor);

        var client = TestData.CreateAuthenticatedClient(_factory, editorToken);
        var response = await client.PutAsJsonAsync(
            $"/comments/{comment.Id}",
            new UpdateCommentRequest("Updated"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Subscriber_CanDeleteOwnComment()
    {
        var (writer, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriber, token) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);

        var client = TestData.CreateAuthenticatedClient(_factory, token);
        var response = await client.DeleteAsync($"/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Editor_CanDeleteAnyComment()
    {
        var (writer, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriber, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);
        var (_, editorToken) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Editor);

        var client = TestData.CreateAuthenticatedClient(_factory, editorToken);
        var response = await client.DeleteAsync($"/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Writer_CanDeleteCommentsOnOwnArticle()
    {
        var (writer, token) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriber, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);

        var client = TestData.CreateAuthenticatedClient(_factory, token);
        var response = await client.DeleteAsync($"/articles/{article.Id}/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Writer_CannotDeleteCommentsOnOtherArticles()
    {
        var (writerA, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var (writerB, tokenB) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writerA.Id);
        var (subscriber, _) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);

        var client = TestData.CreateAuthenticatedClient(_factory, tokenB);
        var response = await client.DeleteAsync($"/articles/{article.Id}/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
