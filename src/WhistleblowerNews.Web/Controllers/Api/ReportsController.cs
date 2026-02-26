using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WhistleblowerNews.Application.Authorization;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Application.Reports;
using WhistleblowerNews.Web.Infrastructure;

namespace WhistleblowerNews.Web.Controllers.Api;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly ReportService _reports;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ReportService reports, ILogger<ReportsController> logger)
    {
        _reports = reports;
        _logger = logger;
    }

    [HttpPost]
    [EnableRateLimiting("report-submit-policy")]
    public async Task<ActionResult<CreateReportResponse>> CreateReport(
        [FromBody] CreateReportRequest request,
        CancellationToken ct)
    {
        var result = await _reports.CreateReportAsync(request, ct);

        return result.Status switch
        {
            ResultStatus.Created => CreatedAtAction(nameof(GetReport), new { caseId = result.Value!.CaseId }, result.Value),
            ResultStatus.BadRequest => BadRequest(result.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("{caseId:guid}")]
    [EnableRateLimiting("reporter-token-policy")]
    public async Task<ActionResult<ReportDetailsDto>> GetReport(
        Guid caseId,
        CancellationToken ct)
    {
        if (Request.Query.ContainsKey("token"))
        {
            _logger.LogWarning("Reporter token supplied via query string is not allowed.");
            return Problem(
                title: "Reporter token cannot be supplied via query string.",
                detail: "Send the reporter token in the X-Reporter-Token header.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var token = Request.Headers["X-Reporter-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized("Reporter token is required.");

        var result = await _reports.GetReportForReporterAsync(caseId, token, ct);
        return ToActionResult(result);
    }

    [HttpPost("{caseId:guid}/request-info")]
    [Authorize(Policy = AuthorizationPolicies.IsInvestigator)]
    public async Task<IActionResult> RequestInfo(
        Guid caseId,
        [FromBody] RequestInfoRequest request,
        CancellationToken ct)
    {
        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _reports.RequestInfoAsync(User, caseId, request, auditContext, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{caseId:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.IsInvestigator)]
    public async Task<IActionResult> UpdateStatus(
        Guid caseId,
        [FromBody] UpdateReportStatusRequest request,
        CancellationToken ct)
    {
        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _reports.UpdateStatusAsync(User, caseId, request, auditContext, ct);
        return ToActionResult(result);
    }

    private ActionResult<T> ToActionResult<T>(ServiceResult<T> result)
    {
        return result.Status switch
        {
            ResultStatus.Ok => Ok(result.Value),
            ResultStatus.BadRequest => BadRequest(result.Error),
            ResultStatus.NotFound => NotFound(),
            ResultStatus.Unauthorized => Unauthorized(),
            ResultStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private IActionResult ToActionResult(ServiceResult result)
    {
        return result.Status switch
        {
            ResultStatus.Ok => Ok(),
            ResultStatus.NoContent => NoContent(),
            ResultStatus.BadRequest => BadRequest(result.Error),
            ResultStatus.NotFound => NotFound(),
            ResultStatus.Unauthorized => Unauthorized(),
            ResultStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
