using System.Net;
using Microsoft.Extensions.DependencyInjection;
using WhistleblowerNews.Domain;
using WhistleblowerNews.Infrastructure;

namespace WhistleblowerNews.Tests;

public sealed class AuditTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuditTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthorizationDenied_IsAudited()
    {
        var (writerA, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var (writerB, writerClientB) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writerA.Id);
        var (subscriber, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        var comment = await TestData.CreateCommentAsync(_factory, article.Id, subscriber.Id);

        var response = await writerClientB.DeleteAsync($"/api/articles/{article.Id}/comments/{comment.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = db.AuditLogEntries.FirstOrDefault(a => a.EventType == AuditEventType.AuthorizationDenied);

        Assert.NotNull(audit);
        Assert.Equal(AuditOutcome.Denied, audit!.Outcome);
    }
}
