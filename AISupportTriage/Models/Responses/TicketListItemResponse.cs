using AISupportTriage.Models.Enums;

namespace AISupportTriage.Models.Responses;

public sealed record TicketListItemResponse
{
    public int Id { get; init; }
    public required string Title { get; init; }
    public TicketStatus Status { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
