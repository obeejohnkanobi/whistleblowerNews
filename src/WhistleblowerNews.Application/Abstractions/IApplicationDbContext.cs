using Microsoft.EntityFrameworkCore;
using WhistleblowerNews.Domain;

namespace WhistleblowerNews.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Article> Articles { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Report> Reports { get; }
    DbSet<ReportMessage> ReportMessages { get; }
    DbSet<ReporterSecret> ReporterSecrets { get; }
    DbSet<InvestigatorAssignment> InvestigatorAssignments { get; }
    DbSet<AuditLogEntry> AuditLogEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
