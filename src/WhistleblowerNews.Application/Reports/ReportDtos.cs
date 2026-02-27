namespace WhistleblowerNews.Application.Reports;

public sealed record CreateReportRequest(string Title, string Description);

public sealed record CreateReportResponse(Guid CaseId, string ReporterToken);

public sealed record RotateTokenRequest(string CurrentToken);

public sealed record RotateTokenResponse(string NewReporterToken);

public sealed record ReportSummaryDto(
    Guid CaseId,
    string Title,
    string Status,
    DateTime CreatedAt);

public sealed record ReportMessageDto(string SenderType, string Content, DateTime CreatedAt);

public sealed record ReportDetailsDto(
    Guid CaseId,
    string Title,
    string Description,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<ReportMessageDto> Messages);

public sealed record RequestInfoRequest(string Content);

public sealed record UpdateReportStatusRequest(string Status);
