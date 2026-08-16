using AISupportTriage.Models.Enums;

namespace AISupportTriage.Models.Entities;

public class SupportTicket
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    public ICollection<TriageResult> TriageResults { get; set; } = [];
    public ICollection<TicketStatusHistory> StatusHistory { get; set; } = [];
}
