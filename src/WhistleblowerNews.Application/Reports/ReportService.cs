using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WhistleblowerNews.Application.Auditing;
using WhistleblowerNews.Application.Abstractions;
using WhistleblowerNews.Application.Authorization;
using WhistleblowerNews.Application.Common;
using WhistleblowerNews.Application.Services;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Application.Reports;

public sealed class ReportService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuthorizationService _authorization;
    private readonly IAuditService _audit;

    public ReportService(IApplicationDbContext db, IAuthorizationService authorization, IAuditService audit)
    {
        _db = db;
        _authorization = authorization;
        _audit = audit;
    }

    public async Task<ServiceResult<CreateReportResponse>> CreateReportAsync(
        CreateReportRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            return ServiceResult<CreateReportResponse>.BadRequest("Title and description are required.");

        var caseId = Guid.NewGuid();
        var report = new Report(caseId, request.Title.Trim(), request.Description.Trim());

        var token = GenerateReporterToken();
        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.HashPassword(token, salt);
        var secret = new ReporterSecret(caseId, $"{salt}${hash}");

        _db.Reports.Add(report);
        _db.ReporterSecrets.Add(secret);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<CreateReportResponse>.Created(new CreateReportResponse(caseId, token));
    }

    public async Task<ServiceResult<ReportDetailsDto>> GetReportForReporterAsync(
        Guid caseId,
        string reporterToken,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reporterToken))
            return ServiceResult<ReportDetailsDto>.Unauthorized("Reporter token is required.");

        var report = await _db.Reports
            .AsNoTracking()
            .Include(r => r.Messages)
            .Include(r => r.ReporterSecret)
            .SingleOrDefaultAsync(r => r.CaseId == caseId, ct);

        if (report is null)
            return ServiceResult<ReportDetailsDto>.NotFound("Report not found.");

        if (report.ReporterSecret is null || !VerifyReporterToken(reporterToken, report.ReporterSecret.SecretHash))
            return ServiceResult<ReportDetailsDto>.Forbidden("Invalid reporter token.");

        var dto = MapReportDetails(report);
        return ServiceResult<ReportDetailsDto>.Ok(dto);
    }

    public async Task<ServiceResult<ReportDetailsDto>> GetReportForInvestigatorAsync(
        ClaimsPrincipal user,
        Guid caseId,
        CancellationToken ct)
    {
        var auth = await _authorization.AuthorizeAsync(user, null, AuthorizationPolicies.IsInvestigator);
        if (!auth.Succeeded)
            return AuthFailure<ReportDetailsDto>(user);

        var report = await _db.Reports
            .AsNoTracking()
            .Include(r => r.Messages)
            .SingleOrDefaultAsync(r => r.CaseId == caseId, ct);

        if (report is null)
            return ServiceResult<ReportDetailsDto>.NotFound("Report not found.");

        var userId = AuthorizationHelpers.GetUserId(user);
        if (!userId.HasValue)
            return ServiceResult<ReportDetailsDto>.Unauthorized("Authentication required.");

        if (!user.IsInRole(UserRole.Editor.ToString()))
        {
            var assignment = await _db.InvestigatorAssignments
                .AsNoTracking()
                .SingleOrDefaultAsync(a => a.CaseId == caseId, ct);

            if (assignment is null || assignment.InvestigatorUserId != userId.Value)
                return ServiceResult<ReportDetailsDto>.Forbidden("Not assigned to this case.");
        }

        return ServiceResult<ReportDetailsDto>.Ok(MapReportDetails(report));
    }

    public async Task<ServiceResult<IReadOnlyList<ReportSummaryDto>>> GetReportsForInvestigatorAsync(
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var auth = await _authorization.AuthorizeAsync(user, null, AuthorizationPolicies.IsInvestigator);
        if (!auth.Succeeded)
            return AuthFailure<IReadOnlyList<ReportSummaryDto>>(user);

        var userId = AuthorizationHelpers.GetUserId(user);
        if (!userId.HasValue)
            return ServiceResult<IReadOnlyList<ReportSummaryDto>>.Unauthorized("Authentication required.");

        IQueryable<Report> query = _db.Reports.AsNoTracking();

        if (!user.IsInRole(UserRole.Editor.ToString()))
        {
            query = from report in _db.Reports.AsNoTracking()
                    join assignment in _db.InvestigatorAssignments.AsNoTracking()
                        on report.CaseId equals assignment.CaseId
                    where assignment.InvestigatorUserId == userId.Value
                    select report;
        }

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReportSummaryDto(
                r.CaseId,
                r.Title,
                r.Status.ToString(),
                r.CreatedAt))
            .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<ReportSummaryDto>>.Ok(items);
    }

    public async Task<ServiceResult> RequestInfoAsync(
        ClaimsPrincipal user,
        Guid caseId,
        RequestInfoRequest request,
        AuditContext auditContext,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return ServiceResult.BadRequest("Content is required.");

        var auth = await _authorization.AuthorizeAsync(user, null, AuthorizationPolicies.IsInvestigator);
        if (!auth.Succeeded)
            return AuthFailure(user);

        var report = await _db.Reports.SingleOrDefaultAsync(r => r.CaseId == caseId, ct);
        if (report is null)
            return ServiceResult.NotFound("Report not found.");

        var userId = AuthorizationHelpers.GetUserId(user);
        if (!userId.HasValue)
            return ServiceResult.Unauthorized("Authentication required.");

        if (!user.IsInRole(UserRole.Editor.ToString()))
        {
            var assignmentOk = await EnsureAssignmentAsync(report.CaseId, userId.Value, ct);
            if (!assignmentOk)
                return ServiceResult.Forbidden("Not assigned to this case.");
        }

        if (!IsValidTransition(report.Status, ReportStatus.WaitingForReporter))
            return ServiceResult.BadRequest($"Invalid status transition from {report.Status} to {ReportStatus.WaitingForReporter}.");

        var message = new ReportMessage(caseId, ReportSenderType.Investigator, request.Content.Trim());
        _db.ReportMessages.Add(message);

        report.UpdateStatus(ReportStatus.WaitingForReporter);

        await _db.SaveChangesAsync(ct);

        await _audit.Success(
            AuditEventType.ReportInfoRequested,
            "Report",
            caseId.ToString(),
            caseId,
            auditContext,
            ct);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateStatusAsync(
        ClaimsPrincipal user,
        Guid caseId,
        UpdateReportStatusRequest request,
        AuditContext auditContext,
        CancellationToken ct)
    {
        if (!Enum.TryParse<ReportStatus>(request.Status, true, out var status))
            return ServiceResult.BadRequest("Invalid status.");

        var auth = await _authorization.AuthorizeAsync(user, null, AuthorizationPolicies.IsInvestigator);
        if (!auth.Succeeded)
            return AuthFailure(user);

        var report = await _db.Reports.SingleOrDefaultAsync(r => r.CaseId == caseId, ct);
        if (report is null)
            return ServiceResult.NotFound("Report not found.");

        var userId = AuthorizationHelpers.GetUserId(user);
        if (!userId.HasValue)
            return ServiceResult.Unauthorized("Authentication required.");

        if (!user.IsInRole(UserRole.Editor.ToString()))
        {
            var assignmentOk = await EnsureAssignmentAsync(report.CaseId, userId.Value, ct);
            if (!assignmentOk)
                return ServiceResult.Forbidden("Not assigned to this case.");
        }

        if (!IsValidTransition(report.Status, status))
            return ServiceResult.BadRequest($"Invalid status transition from {report.Status} to {status}.");

        report.UpdateStatus(status);

        await _db.SaveChangesAsync(ct);

        await _audit.Success(
            AuditEventType.ReportStatusChanged,
            "Report",
            caseId.ToString(),
            caseId,
            auditContext,
            ct);

        return ServiceResult.Ok();
    }

    private static ReportDetailsDto MapReportDetails(Report report)
    {
        var messages = report.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ReportMessageDto(
                m.SenderType.ToString(),
                m.Content,
                m.CreatedAt))
            .ToList();

        return new ReportDetailsDto(
            report.CaseId,
            report.Title,
            report.Description,
            report.Status.ToString(),
            report.CreatedAt,
            messages);
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

    private static bool IsValidTransition(ReportStatus from, ReportStatus to)
    {
        if (from == to)
            return false;

        return from switch
        {
            ReportStatus.Open => to == ReportStatus.InReview,
            ReportStatus.InReview => to == ReportStatus.WaitingForReporter || to == ReportStatus.Closed,
            ReportStatus.WaitingForReporter => to == ReportStatus.InReview || to == ReportStatus.Closed,
            ReportStatus.Closed => false,
            _ => false
        };
    }

    private static ServiceResult<T> AuthFailure<T>(ClaimsPrincipal user)
    {
        return IsAuthenticated(user)
            ? ServiceResult<T>.Forbidden("Forbidden.")
            : ServiceResult<T>.Unauthorized("Authentication required.");
    }

    private static ServiceResult AuthFailure(ClaimsPrincipal user)
    {
        return IsAuthenticated(user)
            ? ServiceResult.Forbidden("Forbidden.")
            : ServiceResult.Unauthorized("Authentication required.");
    }

    private static bool IsAuthenticated(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true;
}
