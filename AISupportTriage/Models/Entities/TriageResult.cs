using AISupportTriage.Models.Enums;

namespace AISupportTriage.Models.Entities;

public class TriageResult
{
    public int Id { get; set; }
    public int SupportTicketId { get; set; }
    public TicketCategory Category { get; set; }
    public TicketSeverity Severity { get; set; }
    public required string Summary { get; set; }
    public required string LikelyCause { get; set; }
    public int? KnownIssueId { get; set; }
    public DateTime AnalyzedAtUtc { get; set; }

    public SupportTicket? SupportTicket { get; set; }
    public KnownIssue? KnownIssue { get; set; }
    public ICollection<TriageRecommendation> Recommendations { get; set; } = [];
}
