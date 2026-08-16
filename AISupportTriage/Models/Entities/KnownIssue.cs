using AISupportTriage.Models.Enums;

namespace AISupportTriage.Models.Entities;

public class KnownIssue
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public TicketCategory Category { get; set; }
    public required string Symptoms { get; set; }
    public required string Resolution { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<TriageResult> TriageResults { get; set; } = [];
}
