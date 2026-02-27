using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Application.Reports;
using WhistleblowerNews.Web.Infrastructure;
using WhistleblowerNews.Web.Models.Reports;

namespace WhistleblowerNews.Web.Areas.Whistleblower.Controllers;

[Area("Whistleblower")]
[AllowAnonymous]
public sealed class ReportsController : Controller
{
    private readonly ReportService _reports;

    public ReportsController(ReportService reports)
    {
        _reports = reports;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ReportCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReportCreateViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var auditContext = AuditContextFactory.FromHttpContext(HttpContext);
        var result = await _reports.CreateReportAsync(
            new CreateReportRequest(model.Title, model.Description),
            auditContext,
            ct);

        if (result.Status == ResultStatus.Created)
            return View("Created", result.Value);

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to submit report.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Track()
    {
        return View(new ReportTrackViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Track(ReportTrackViewModel model, CancellationToken ct)
    {
        if (!Guid.TryParse(model.CaseId, out var caseId))
        {
            ModelState.AddModelError(nameof(model.CaseId), "Case ID is invalid.");
            return View(model);
        }

        var result = await _reports.GetReportForReporterAsync(caseId, model.ReporterToken, ct);
        if (result.Status == ResultStatus.Ok)
            return View("TrackResult", result.Value);

        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        if (result.Status == ResultStatus.Forbidden || result.Status == ResultStatus.Unauthorized)
        {
            ModelState.AddModelError(string.Empty, "Invalid token or case ID.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Unable to retrieve report.");
        return View(model);
    }
}
