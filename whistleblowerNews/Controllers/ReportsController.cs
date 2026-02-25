using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using whistleblowerNews.Application.Reports;
using whistleblowerNews.Authorization;
using whistleblowerNews.Domain;
using whistleblowerNews.Infrastructure;
using whistleblowerNews.Services;

namespace whistleblowerNews.Controllers;

[ApiController]
[Route("reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ApplicationDbContext db, ILogger<ReportsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // POST /reports (anonymous)
    [HttpPost]
    [EnableRateLimiting("report-submit-policy")]
    public async Task<ActionResult<CreateReportResponse>> CreateReport(
        [FromBody] CreateReportRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("Title and description are required.");

        var caseId = Guid.NewGuid();
        var report = new Report(caseId, request.Title.Trim(), request.Description.Trim());

        var token = GenerateReporterToken();
        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.HashPassword(token, salt);
        var secret = new ReporterSecret(caseId, $"{salt}${hash}");

        _db.Reports.Add(report);
        _db.ReporterSecrets.Add(secret);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetReport), new { caseId }, new CreateReportResponse(caseId, token));
    }

    // GET /reports/{caseId} (anonymous with token)
    [HttpGet("{caseId:guid}")]
    [EnableRateLimiting("reporter-token-policy")]
    public async Task<ActionResult<ReportDetailsDto>> GetReport(
        Guid caseId,
        [FromQuery] string? token,
        CancellationToken ct)
    {
        var headerToken = Request.Headers["X-Reporter-Token"].FirstOrDefault();
        var tokenValue = !string.IsNullOrWhiteSpace(headerToken) ? headerToken : token;

        if (string.IsNullOrWhiteSpace(tokenValue))
            return Unauthorized("Reporter token is required.");

        if (string.IsNullOrWhiteSpace(headerToken) && !string.IsNullOrWhiteSpace(token))
            _logger.LogWarning("Reporter token supplied via query string is deprecated. Use X-Reporter-Token header.");

        var report = await _db.Reports
            .AsNoTracking()
            .Include(r => r.Messages)
            .Include(r => r.ReporterSecret)
            .SingleOrDefaultAsync(r => r.CaseId == caseId, ct);

        if (report is null)
            return NotFound();

        if (report.ReporterSecret is null || !VerifyReporterToken(tokenValue, report.ReporterSecret.SecretHash))
            return Forbid();

        var messages = report.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ReportMessageDto(
                m.SenderType.ToString(),
                m.Content,
                m.CreatedAt))
            .ToList();

        var dto = new ReportDetailsDto(
            report.CaseId,
            report.Title,
            report.Description,
            report.Status.ToString(),
            report.CreatedAt,
            messages);

        return Ok(dto);
    }

    // POST /reports/{caseId}/request-info (Investigator or Editor)
    [HttpPost("{caseId:guid}/request-info")]
    [Authorize(Policy = AuthorizationPolicies.IsInvestigator)]
    public async Task<IActionResult> RequestInfo(
        Guid caseId,
        [FromBody] RequestInfoRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Content is required.");

        var report = await _db.Reports.SingleOrDefaultAsync(r => r.CaseId == caseId, ct);
        if (report is null)
            return NotFound();

        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (!User.IsInRole(UserRole.Editor.ToString()))
        {
            var assignmentOk = await EnsureAssignmentAsync(report.CaseId, userId.Value, ct);
            if (!assignmentOk)
                return Forbid();
        }

        var message = new ReportMessage(caseId, ReportSenderType.Investigator, request.Content.Trim());
        _db.ReportMessages.Add(message);

        report.UpdateStatus(ReportStatus.WaitingForReporter);

        _db.AuditLogEntries.Add(new AuditLogEntry(caseId, userId.Value, "RequestInfo"));

        await _db.SaveChangesAsync(ct);

        return Ok();
    }

    // PATCH /reports/{caseId}/status (Investigator or Editor)
    [HttpPatch("{caseId:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.IsInvestigator)]
    public async Task<IActionResult> UpdateStatus(
        Guid caseId,
        [FromBody] UpdateReportStatusRequest request,
        CancellationToken ct)
    {
        if (!Enum.TryParse<ReportStatus>(request.Status, true, out var status))
            return BadRequest("Invalid status.");

        var report = await _db.Reports.SingleOrDefaultAsync(r => r.CaseId == caseId, ct);
        if (report is null)
            return NotFound();

        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (!User.IsInRole(UserRole.Editor.ToString()))
        {
            var assignmentOk = await EnsureAssignmentAsync(report.CaseId, userId.Value, ct);
            if (!assignmentOk)
                return Forbid();
        }

        report.UpdateStatus(status);
        _db.AuditLogEntries.Add(new AuditLogEntry(caseId, userId.Value, $"UpdateStatus:{status}"));

        await _db.SaveChangesAsync(ct);

        return Ok();
    }

    private static string GenerateReporterToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes);
    }

    private static bool VerifyReporterToken(string token, string secretHash)
    {
        var parts = secretHash.Split('$');
        if (parts.Length != 2)
            return false;

        var salt = parts[0];
        var expectedHash = parts[1];
        return PasswordHasher.VerifyHash(token, salt, expectedHash);
    }

    private int? GetUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

    private async Task<bool> EnsureAssignmentAsync(Guid caseId, int investigatorUserId, CancellationToken ct)
    {
        var existing = await _db.InvestigatorAssignments
            .SingleOrDefaultAsync(a => a.CaseId == caseId, ct);

        if (existing is null)
        {
            _db.InvestigatorAssignments.Add(new InvestigatorAssignment(caseId, investigatorUserId));
            return true;
        }

        return existing.InvestigatorUserId == investigatorUserId;
    }
}
