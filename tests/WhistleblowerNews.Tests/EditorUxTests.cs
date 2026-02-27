using System.Net;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Tests;

public sealed class EditorUxTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public EditorUxTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EditorArticles_FilterByTitleOrAuthor()
    {
        var (writerA, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var (writerB, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        await TestData.CreateArticleAsync(_factory, writerA.Id, title: "Alpha Title");
        await TestData.CreateArticleAsync(_factory, writerB.Id, title: "Beta Title");

        var (_, editorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Editor);

        var response = await editorClient.GetAsync("/Editor/Articles?query=Alpha");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Alpha Title", html);
        Assert.DoesNotContain("Beta Title", html);
    }

    [Fact]
    public async Task EditorComments_FilterByUsername()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id, title: "News One");
        var (subscriberA, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        var (subscriberB, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        await TestData.CreateCommentAsync(_factory, article.Id, subscriberA.Id, "First comment");
        await TestData.CreateCommentAsync(_factory, article.Id, subscriberB.Id, "Second comment");

        var (_, editorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Editor);

        var response = await editorClient.GetAsync($"/Editor/Comments?username={subscriberA.UserName ?? string.Empty}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(subscriberA.UserName ?? string.Empty, html);
        Assert.DoesNotContain(subscriberB.UserName ?? string.Empty, html);
    }

    [Fact]
    public async Task EditorComments_FilterByArticleTitle()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var articleA = await TestData.CreateArticleAsync(_factory, writer.Id, title: "Editorial One");
        var articleB = await TestData.CreateArticleAsync(_factory, writer.Id, title: "Editorial Two");
        var (subscriber, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        await TestData.CreateCommentAsync(_factory, articleA.Id, subscriber.Id, "Comment for A");
        await TestData.CreateCommentAsync(_factory, articleB.Id, subscriber.Id, "Comment for B");

        var (_, editorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Editor);

        var response = await editorClient.GetAsync("/Editor/Comments?articleTitle=Editorial%20One");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Editorial One", html);
        Assert.DoesNotContain("Editorial Two", html);
    }
}
