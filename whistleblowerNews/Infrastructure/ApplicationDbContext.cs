using Microsoft.EntityFrameworkCore;
using whistleblowerNews.Domain;

namespace whistleblowerNews.Infrastructure;

/// <summary>
/// EF Core database context (Unit of Work).
/// This maps our Domain entities to SQLite tables.
/// </summary>
public sealed class ApplicationDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportMessage> ReportMessages => Set<ReportMessage>();
    public DbSet<ReporterSecret> ReporterSecrets => Set<ReporterSecret>();
    public DbSet<InvestigatorAssignment> InvestigatorAssignments => Set<InvestigatorAssignment>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Article -> Author (User)
        modelBuilder.Entity<Article>()
            .HasOne(a => a.Author)
            .WithMany(u => u.Articles)
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Comment -> Article
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Article)
            .WithMany(a => a.Comments)
            .HasForeignKey(c => c.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Comment -> User
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Report (CaseId as key)
        modelBuilder.Entity<Report>()
            .HasKey(r => r.CaseId);

        modelBuilder.Entity<Report>()
            .Property(r => r.Status)
            .HasConversion<string>();

        // ReportMessage -> Report
        modelBuilder.Entity<ReportMessage>()
            .HasOne(m => m.Report)
            .WithMany(r => r.Messages)
            .HasForeignKey(m => m.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReportMessage>()
            .Property(m => m.SenderType)
            .HasConversion<string>();

        // ReporterSecret -> Report (one-to-one)
        modelBuilder.Entity<ReporterSecret>()
            .HasKey(s => s.CaseId);

        modelBuilder.Entity<ReporterSecret>()
            .HasOne(s => s.Report)
            .WithOne(r => r.ReporterSecret)
            .HasForeignKey<ReporterSecret>(s => s.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // InvestigatorAssignment -> Report
        modelBuilder.Entity<InvestigatorAssignment>()
            .HasOne(a => a.Report)
            .WithMany(r => r.Assignments)
            .HasForeignKey(a => a.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // InvestigatorAssignment -> User
        modelBuilder.Entity<InvestigatorAssignment>()
            .HasOne(a => a.Investigator)
            .WithMany()
            .HasForeignKey(a => a.InvestigatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // AuditLogEntry -> Report
        modelBuilder.Entity<AuditLogEntry>()
            .HasOne(a => a.Report)
            .WithMany(r => r.AuditLogs)
            .HasForeignKey(a => a.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // AuditLogEntry -> User
        modelBuilder.Entity<AuditLogEntry>()
            .HasOne(a => a.Actor)
            .WithMany()
            .HasForeignKey(a => a.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enum mapping (store as text for readability)
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();
    }
}
