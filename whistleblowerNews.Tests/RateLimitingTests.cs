using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using whistleblowerNews.Application.Reports;

namespace whistleblowerNews.Tests;

public sealed class RateLimitingTests
{
    [Fact]
    public async Task ReportSubmit_IsRateLimited()
    {
        using var baseFactory = new TestWebApplicationFactory();
        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:ReportSubmit:PermitLimit"] = "2",
                    ["RateLimiting:ReportSubmit:WindowSeconds"] = "60"
                });
            });
        });

        var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync(
            "/reports",
            new CreateReportRequest("Title 1", "Description"));
        var second = await client.PostAsJsonAsync(
            "/reports",
            new CreateReportRequest("Title 2", "Description"));
        var third = await client.PostAsJsonAsync(
            "/reports",
            new CreateReportRequest("Title 3", "Description"));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal((HttpStatusCode)429, third.StatusCode);
    }

    [Fact]
    public async Task ReporterTokenEndpoint_IsRateLimited()
    {
        using var baseFactory = new TestWebApplicationFactory();
        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:ReporterToken:PermitLimit"] = "2",
                    ["RateLimiting:ReporterToken:WindowSeconds"] = "60",
                    ["RateLimiting:ReportSubmit:PermitLimit"] = "1000",
                    ["RateLimiting:ReportSubmit:WindowSeconds"] = "60"
                });
            });
        });

        var client = factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/reports",
            new CreateReportRequest("Title", "Description"));

        var payload = await create.Content.ReadFromJsonAsync<CreateReportResponse>();
        Assert.NotNull(payload);

        var request1 = CreateReportRequest(payload!.CaseId, payload.ReporterToken);
        var request2 = CreateReportRequest(payload.CaseId, payload.ReporterToken);
        var request3 = CreateReportRequest(payload.CaseId, payload.ReporterToken);

        var first = await client.SendAsync(request1);
        var second = await client.SendAsync(request2);
        var third = await client.SendAsync(request3);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal((HttpStatusCode)429, third.StatusCode);
    }

    private static HttpRequestMessage CreateReportRequest(Guid caseId, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/reports/{caseId}");
        request.Headers.Add("X-Reporter-Token", token);
        return request;
    }
}