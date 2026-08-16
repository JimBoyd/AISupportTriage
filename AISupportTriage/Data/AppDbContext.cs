using Microsoft.EntityFrameworkCore;
using AISupportTriage.Models.Entities;

namespace AISupportTriage.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<TriageResult> TriageResults => Set<TriageResult>();
    public DbSet<TriageRecommendation> TriageRecommendations => Set<TriageRecommendation>();
    public DbSet<KnownIssue> KnownIssues => Set<KnownIssue>();
    public DbSet<TicketStatusHistory> TicketStatusHistories => Set<TicketStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SupportTicket configuration
        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();

            entity.HasMany(e => e.TriageResults)
                .WithOne(e => e.SupportTicket)
                .HasForeignKey(e => e.SupportTicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.StatusHistory)
                .WithOne(e => e.SupportTicket)
                .HasForeignKey(e => e.SupportTicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TriageResult configuration
        modelBuilder.Entity<TriageResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired();
            entity.Property(e => e.Severity).IsRequired();
            entity.Property(e => e.Summary).IsRequired();
            entity.Property(e => e.LikelyCause).IsRequired();
            entity.Property(e => e.AnalyzedAtUtc).IsRequired();

            entity.HasOne(e => e.KnownIssue)
                .WithMany(e => e.TriageResults)
                .HasForeignKey(e => e.KnownIssueId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Recommendations)
                .WithOne(e => e.TriageResult)
                .HasForeignKey(e => e.TriageResultId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TriageRecommendation configuration
        modelBuilder.Entity<TriageRecommendation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sequence).IsRequired();
            entity.Property(e => e.Description).IsRequired();
        });

        // KnownIssue configuration
        modelBuilder.Entity<KnownIssue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Category).IsRequired();
            entity.Property(e => e.Symptoms).IsRequired();
            entity.Property(e => e.Resolution).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
        });

        // TicketStatusHistory configuration
        modelBuilder.Entity<TicketStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PreviousStatus).IsRequired();
            entity.Property(e => e.NewStatus).IsRequired();
            entity.Property(e => e.ChangedAtUtc).IsRequired();
        });
    }
}
