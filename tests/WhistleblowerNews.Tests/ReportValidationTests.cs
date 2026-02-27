using System.Net;
using System.Net.Http.Json;
using WhistleblowerNews.Application.Reports;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Tests;

public sealed class ReportValidationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ReportValidationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateReport_EmptyTitle_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("", "Some description"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateReport_EmptyDescription_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Some title", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReport_NonExistentCase_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/reports/{Guid.NewGuid()}");
        request.Headers.Add("X-Reporter-Token", "some-token");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RotateToken_NonExistentCase_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/reports/{Guid.NewGuid()}/rotate-token",
            new RotateTokenRequest("some-token"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RequestInfo_NonExistentCase_ReturnsNotFound()
    {
        var (_, investigatorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Investigator);

        var response = await investigatorClient.PostAsJsonAsync(
            $"/api/reports/{Guid.NewGuid()}/request-info",
            new RequestInfoRequest("Need more info"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_NonExistentCase_ReturnsNotFound()
    {
        var (_, investigatorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Investigator);

        var response = await investigatorClient.PatchAsJsonAsync(
            $"/api/reports/{Guid.NewGuid()}/status",
            new UpdateReportStatusRequest("InReview"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatusString_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));
        var payload = await createResponse.Content.ReadFromJsonAsync<CreateReportResponse>();

        var (_, investigatorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Investigator);

        var response = await investigatorClient.PatchAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/status",
            new UpdateReportStatusRequest("InvalidStatus"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RequestInfo_EmptyContent_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));
        var payload = await createResponse.Content.ReadFromJsonAsync<CreateReportResponse>();

        var (_, investigatorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Investigator);

        await investigatorClient.PatchAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/status",
            new UpdateReportStatusRequest("InReview"));

        var response = await investigatorClient.PostAsJsonAsync(
            $"/api/reports/{payload.CaseId}/request-info",
            new RequestInfoRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Editor_CanRequestInfo()
    {
        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(
            "/api/reports",
            new CreateReportRequest("Title", "Description"));
        var payload = await createResponse.Content.ReadFromJsonAsync<CreateReportResponse>();

        var (_, editorClient) = await TestData.CreateUserWithClientAsync(_factory, UserRole.Editor);

        await editorClient.PatchAsJsonAsync(
            $"/api/reports/{payload!.CaseId}/status",
            new UpdateReportStatusRequest("InReview"));

        var response = await editorClient.PostAsJsonAsync(
            $"/api/reports/{payload.CaseId}/request-info",
            new RequestInfoRequest("Need more details"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
