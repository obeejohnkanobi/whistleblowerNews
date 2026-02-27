using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using WhistleblowerNews.Application.Articles;
using WhistleblowerNews.Application.Authentication;
using WhistleblowerNews.Application.Comments;
using WhistleblowerNews.Application.Reports;
using WhistleblowerNews.Domain;
using WhistleblowerNews.Infrastructure;

namespace WhistleblowerNews.Tests;

public sealed class AuditEventTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuditEventTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ArticleCreated_IsAudited()
    {
        var (writer, writerClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);

        var response = await writerClient.PostAsJsonAsync(
            "/api/articles",
            new CreateArticleRequest("Test Title", "Test Content"));

        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = db.AuditLogEntries.FirstOrDefault(a =>
            a.EventType == AuditEventType.ArticleCreated &&
            a.ActorUserId == writer.Id);

        Assert.NotNull(audit);
        Assert.Equal(AuditOutcome.Success, audit!.Outcome);
        Assert.Equal("Article", audit.TargetType);
    }

    [Fact]
    public async Task CommentCreated_IsAudited()
    {
        var (writer, _) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var article = await TestData.CreateArticleAsync(_factory, writer.Id);
        var (subscriber, subscriberClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);

        var response = await subscriberClient.PostAsJsonAsync(
            $"/api/articles/{article.Id}/comments",
            new CreateCommentRequest("Great article!"));

        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = db.AuditLogEntries.FirstOrDefault(a =>
            a.EventType == AuditEventType.CommentCreated &&
            a.ActorUserId == subscriber.Id);

        Assert.NotNull(audit);
        Assert.Equal(AuditOutcome.Success, audit!.Outcome);
        Assert.Equal("Comment", audit.TargetType);
    }

    [Fact]
    public async Task ReportCreated_IsAudited()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Safety Issue", "Details about the issue"));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CreateReportResponse>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = db.AuditLogEntries.FirstOrDefault(a =>
            a.EventType == AuditEventType.ReportCreated &&
            a.TargetId == payload!.CaseId.ToString());

        Assert.NotNull(audit);
        Assert.Equal(AuditOutcome.Success, audit!.Outcome);
        Assert.Equal("Report", audit.TargetType);
    }

    [Fact]
    public async Task TokenRotated_IsAudited()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));
        var createPayload = await createResponse.Content.ReadFromJsonAsync<CreateReportResponse>();

        var rotateResponse = await client.PostAsJsonAsync(
            $"/api/reports/{createPayload!.CaseId}/rotate-token",
            new RotateTokenRequest(createPayload.ReporterToken));

        rotateResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = db.AuditLogEntries.FirstOrDefault(a =>
            a.EventType == AuditEventType.TokenRotated &&
            a.TargetId == createPayload.CaseId.ToString());

        Assert.NotNull(audit);
        Assert.Equal(AuditOutcome.Success, audit!.Outcome);
        Assert.Equal("Report", audit.TargetType);
    }

    [Fact]
    public async Task LoginSucceeded_IsAudited()
    {
        var username = $"writer_{Guid.NewGuid():N}";
        var user = await TestData.CreateUserAsync(_factory, UserRole.Writer, username, "password1");

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, "password1"));

        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = db.AuditLogEntries.FirstOrDefault(a =>
            a.EventType == AuditEventType.LoginSucceeded &&
            a.TargetId == user.Id.ToString());

        Assert.NotNull(audit);
        Assert.Equal(AuditOutcome.Success, audit!.Outcome);
    }
}
