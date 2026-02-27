using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using WhistleblowerNews.Application.Reports;
using WhistleblowerNews.Domain;
using WhistleblowerNews.Infrastructure;

namespace WhistleblowerNews.Tests;

public sealed class ReportTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ReportTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateReport_ReturnsCaseIdAndToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Unsafe behavior", "Details"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CreateReportResponse>();
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload!.CaseId);
        Assert.False(string.IsNullOrWhiteSpace(payload.ReporterToken));
    }

    [Fact]
    public async Task GetReport_RequiresToken()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));

        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();
        var response = await client.GetAsync($"/api/reports/{payload!.CaseId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetReport_QueryToken_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));

        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();
        var response = await client.GetAsync($"/api/reports/{payload!.CaseId}?token={payload.ReporterToken}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
        Assert.True(problem.Extensions.ContainsKey("correlationId"));
    }

    [Fact]
    public async Task GetReport_InvalidToken_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));

        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();
        var request = CreateReportRequest(payload!.CaseId, "wrong");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetReport_WithValidToken_ReturnsStatus()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));

        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();
        var request = CreateReportRequest(payload!.CaseId, payload.ReporterToken);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<ReportDetailsDto>();
        Assert.NotNull(report);
        Assert.Equal("Open", report!.Status);
    }

    [Fact]
    public async Task RequestInfo_RequiresInvestigatorOrEditor()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));
        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        var guestResponse = await client.PostAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/request-info",
            new RequestInfoRequest("Info"));
        Assert.Equal(HttpStatusCode.Unauthorized, guestResponse.StatusCode);

        var (_, subscriberClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        var subscriberResponse = await subscriberClient.PostAsJsonAsync(
            $"/api/reports/{payload.CaseId}/request-info",
            new RequestInfoRequest("Info"));

        Assert.Equal(HttpStatusCode.Forbidden, subscriberResponse.StatusCode);
    }

    [Fact]
    public async Task Investigator_RequestInfo_AddsMessageAndAudit()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));
        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        var (_, investigatorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Investigator);

        var startReview = await investigatorClient.PatchAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/status",
            new UpdateReportStatusRequest("InReview"));
        Assert.Equal(HttpStatusCode.OK, startReview.StatusCode);

        var requestInfo = await investigatorClient.PostAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/request-info",
            new RequestInfoRequest("Please provide more details"));

        Assert.Equal(HttpStatusCode.OK, requestInfo.StatusCode);

        var reportRequest = CreateReportRequest(payload.CaseId, payload.ReporterToken);
        var reportResponse = await client.SendAsync(reportRequest);

        var report = await reportResponse.Content.ReadFromJsonAsync<ReportDetailsDto>();
        Assert.NotNull(report);
        Assert.Contains(report!.Messages, m => m.SenderType == "Investigator");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = db.AuditLogEntries.FirstOrDefault(a =>
            a.CaseId == payload.CaseId &&
            a.EventType == AuditEventType.ReportInfoRequested);
        Assert.NotNull(audit);
        Assert.Equal(AuditOutcome.Success, audit!.Outcome);
    }

    [Fact]
    public async Task UpdateStatus_CreatesAuditEntry()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));
        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        var (_, editorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Editor);

        var startReview = await editorClient.PatchAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/status",
            new UpdateReportStatusRequest("InReview"));
        Assert.Equal(HttpStatusCode.OK, startReview.StatusCode);

        var update = await editorClient.PatchAsJsonAsync(
            $"/api/reports/{payload.CaseId}/status",
            new UpdateReportStatusRequest("Closed"));

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = db.AuditLogEntries.FirstOrDefault(a =>
            a.CaseId == payload.CaseId &&
            a.EventType == AuditEventType.ReportStatusChanged);

        Assert.NotNull(audit);
        Assert.Equal(AuditOutcome.Success, audit!.Outcome);
    }

    [Fact]
    public async Task Investigator_Assignment_RestrictsOtherInvestigators()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));
        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        var (_, clientA) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Investigator);
        var (_, clientB) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Investigator);
        var (_, editorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Editor);

        var startReview = await clientA.PatchAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/status",
            new UpdateReportStatusRequest("InReview"));
        Assert.Equal(HttpStatusCode.OK, startReview.StatusCode);

        var first = await clientA.PostAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/request-info",
            new RequestInfoRequest("First investigator"));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var updateStatus = await clientB.PatchAsJsonAsync(
            $"/api/reports/{payload.CaseId}/status",
            new UpdateReportStatusRequest("Closed"));

        Assert.Equal(HttpStatusCode.Forbidden, updateStatus.StatusCode);

        var editorReview = await editorClient.PatchAsJsonAsync(
            $"/api/reports/{payload.CaseId}/status",
            new UpdateReportStatusRequest("InReview"));
        Assert.Equal(HttpStatusCode.OK, editorReview.StatusCode);

        var editorUpdate = await editorClient.PatchAsJsonAsync(
            $"/api/reports/{payload.CaseId}/status",
            new UpdateReportStatusRequest("Closed"));

        Assert.Equal(HttpStatusCode.OK, editorUpdate.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));
        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        var (_, editorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Editor);

        var update = await editorClient.PatchAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/status",
            new UpdateReportStatusRequest("Closed"));

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    private static HttpRequestMessage CreateReportRequest(Guid caseId, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/reports/{caseId}");
        request.Headers.Add("X-Reporter-Token", token);
        return request;
    }

    [Fact]
    public async Task GetReport_ExpiredToken_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));

        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        // Backdate the token's CreatedAt beyond the 90-day expiry window
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var secret = db.ReporterSecrets.Single(s => s.CaseId == payload!.CaseId);
            db.Entry(secret).Property("CreatedAt").CurrentValue = DateTime.UtcNow.AddDays(-91);
            await db.SaveChangesAsync();
        }

        var request = CreateReportRequest(payload!.CaseId, payload.ReporterToken);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RotateToken_WithValidToken_ReturnsNewToken()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));

        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        var rotateResponse = await client.PostAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/rotate-token",
            new RotateTokenRequest(payload.ReporterToken));

        Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);

        var rotated = await rotateResponse.Content.ReadFromJsonAsync<RotateTokenResponse>();
        Assert.NotNull(rotated);
        Assert.False(string.IsNullOrWhiteSpace(rotated!.NewReporterToken));
        Assert.NotEqual(payload.ReporterToken, rotated.NewReporterToken);
    }

    [Fact]
    public async Task RotateToken_OldTokenNoLongerWorks()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));

        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        await client.PostAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/rotate-token",
            new RotateTokenRequest(payload.ReporterToken));

        // Old token should now be invalid
        var request = CreateReportRequest(payload.CaseId, payload.ReporterToken);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RotateToken_WithInvalidToken_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));

        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        var rotateResponse = await client.PostAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/rotate-token",
            new RotateTokenRequest("wrongtoken"));

        Assert.Equal(HttpStatusCode.Forbidden, rotateResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_RequiresInvestigatorOrEditor()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));
        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        // Guest is rejected
        var guestResponse = await client.PatchAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/status",
            new UpdateReportStatusRequest("InReview"));
        Assert.Equal(HttpStatusCode.Unauthorized, guestResponse.StatusCode);

        // Subscriber is rejected
        var (_, subscriberClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Subscriber);
        var subscriberResponse = await subscriberClient.PatchAsJsonAsync(
            $"/api/reports/{payload.CaseId}/status",
            new UpdateReportStatusRequest("InReview"));
        Assert.Equal(HttpStatusCode.Forbidden, subscriberResponse.StatusCode);

        // Writer is rejected
        var (_, writerClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Writer);
        var writerResponse = await writerClient.PatchAsJsonAsync(
            $"/api/reports/{payload.CaseId}/status",
            new UpdateReportStatusRequest("InReview"));
        Assert.Equal(HttpStatusCode.Forbidden, writerResponse.StatusCode);
    }
}
