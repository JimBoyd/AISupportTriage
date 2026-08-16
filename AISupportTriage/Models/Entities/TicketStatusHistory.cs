using AISupportTriage.Models.Enums;

namespace AISupportTriage.Models.Entities;

public class TicketStatusHistory
{
    public int Id { get; set; }
    public int SupportTicketId { get; set; }
    public TicketStatus PreviousStatus { get; set; }
    public TicketStatus NewStatus { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public string? Notes { get; set; }

    public SupportTicket? SupportTicket { get; set; }
}
