using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using whistleblowerNews.Application.Reports;
using whistleblowerNews.Domain;
using whistleblowerNews.Infrastructure;

namespace whistleblowerNews.Tests;

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
            "/reports",
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
            "/reports",
            new CreateReportRequest("Title", "Description"));

        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();
        var response = await client.GetAsync($"/reports/{payload!.CaseId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetReport_InvalidToken_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/reports",
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
            "/reports",
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
    public async Task GetReport_QueryToken_StillWorksForBackwardCompatibility()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/reports",
            new CreateReportRequest("Title", "Description"));

        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();
        var response = await client.GetAsync($"/reports/{payload!.CaseId}?token={payload.ReporterToken}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReport_HeaderToken_TakesPrecedenceOverQuery()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/reports",
            new CreateReportRequest("Title", "Description"));

        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/reports/{payload!.CaseId}?token=wrong");
        request.Headers.Add("X-Reporter-Token", payload.ReporterToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RequestInfo_RequiresInvestigatorOrEditor()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/reports",
            new CreateReportRequest("Title", "Description"));
        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        var guestResponse = await client.PostAsJsonAsync(
            $"/reports/{payload!.CaseId}/request-info",
            new RequestInfoRequest("Info"));
        Assert.Equal(HttpStatusCode.Unauthorized, guestResponse.StatusCode);

        var (_, subscriberToken) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Subscriber);
        var subscriberClient = TestData.CreateAuthenticatedClient(_factory, subscriberToken);
        var subscriberResponse = await subscriberClient.PostAsJsonAsync(
            $"/reports/{payload.CaseId}/request-info",
            new RequestInfoRequest("Info"));

        Assert.Equal(HttpStatusCode.Forbidden, subscriberResponse.StatusCode);
    }

    [Fact]
    public async Task Investigator_RequestInfo_AddsMessageAndAudit()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/reports",
            new CreateReportRequest("Title", "Description"));
        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        var (_, token) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Investigator);
        var authClient = TestData.CreateAuthenticatedClient(_factory, token);

        var requestInfo = await authClient.PostAsJsonAsync(
            $"/reports/{payload!.CaseId}/request-info",
            new RequestInfoRequest("Please provide more details"));

        Assert.Equal(HttpStatusCode.OK, requestInfo.StatusCode);

        var reportRequest = CreateReportRequest(payload.CaseId, payload.ReporterToken);
        var reportResponse = await client.SendAsync(reportRequest);

        var report = await reportResponse.Content.ReadFromJsonAsync<ReportDetailsDto>();
        Assert.NotNull(report);
        Assert.Contains(report!.Messages, m => m.SenderType == "Investigator");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audits = db.AuditLogEntries.Count(a => a.CaseId == payload.CaseId);
        Assert.True(audits > 0);
    }

    [Fact]
    public async Task Investigator_Assignment_RestrictsOtherInvestigators()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/reports",
            new CreateReportRequest("Title", "Description"));
        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();

        var (_, tokenA) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Investigator);
        var (_, tokenB) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Investigator);
        var (_, editorToken) = await TestData.CreateUserWithTokenAsync(_factory, UserRole.Editor);

        var clientA = TestData.CreateAuthenticatedClient(_factory, tokenA);
        var clientB = TestData.CreateAuthenticatedClient(_factory, tokenB);
        var editorClient = TestData.CreateAuthenticatedClient(_factory, editorToken);

        var first = await clientA.PostAsJsonAsync(
            $"/reports/{payload!.CaseId}/request-info",
            new RequestInfoRequest("First investigator"));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var updateStatus = await clientB.PatchAsJsonAsync(
            $"/reports/{payload.CaseId}/status",
            new UpdateReportStatusRequest("Closed"));

        Assert.Equal(HttpStatusCode.Forbidden, updateStatus.StatusCode);

        var editorUpdate = await editorClient.PatchAsJsonAsync(
            $"/reports/{payload.CaseId}/status",
            new UpdateReportStatusRequest("Closed"));

        Assert.Equal(HttpStatusCode.OK, editorUpdate.StatusCode);
    }

    private static HttpRequestMessage CreateReportRequest(Guid caseId, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/reports/{caseId}");
        request.Headers.Add("X-Reporter-Token", token);
        return request;
    }
}
