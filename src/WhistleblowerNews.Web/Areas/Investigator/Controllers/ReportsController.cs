using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Application.Authorization;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Application.Reports;
using WhistleblowerNews.Web.Infrastructure;
using WhistleblowerNews.Web.Models.Reports;

namespace WhistleblowerNews.Web.Areas.Investigator.Controllers;

[Area("Investigator")]
[Authorize(Policy = AuthorizationPolicies.IsInvestigator)]
public sealed class ReportsController : Controller
{
    private readonly ReportService _reports;

    public ReportsController(ReportService reports)
    {
        _reports = reports;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _reports.GetReportsForInvestigatorAsync(User, ct);
        if (result.Status == ResultStatus.Unauthorized)
            return Unauthorized();
        if (result.Status == ResultStatus.Forbidden)
            return Forbid();

        return View(result.Value ?? Array.Empty<ReportSummaryDto>());
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid caseId, CancellationToken ct)
    {
        var result = await _reports.GetReportForInvestigatorAsync(User, caseId, ct);
        if (result.Status == ResultStatus.NotFound)
            return NotFound();
        if (result.Status == ResultStatus.Forbidden)
            return Forbid();
        if (result.Status == ResultStatus.Unauthorized)
            return Unauthorized();

        return View(result.Value!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestInfo(Guid caseId, string content, CancellationToken ct)
    {
        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _reports.RequestInfoAsync(User, caseId, new RequestInfoRequest(content), auditContext, ct);
        if (result.Status == ResultStatus.Ok)
            return RedirectToAction(nameof(Details), new { caseId });

        if (result.Status == ResultStatus.Forbidden)
            return Forbid();
        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        return BadRequest(result.Error);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid caseId, ReportStatusViewModel model, CancellationToken ct)
    {
        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _reports.UpdateStatusAsync(User, caseId, new UpdateReportStatusRequest(model.Status), auditContext, ct);
        if (result.Status == ResultStatus.Ok)
            return RedirectToAction(nameof(Details), new { caseId });

        if (result.Status == ResultStatus.Forbidden)
            return Forbid();
        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        return BadRequest(result.Error);
    }
}
