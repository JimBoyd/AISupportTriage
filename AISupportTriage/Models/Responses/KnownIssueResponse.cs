using AISupportTriage.Models.Enums;

namespace AISupportTriage.Models.Responses;

public sealed record KnownIssueResponse
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public TicketCategory Category { get; init; }
    public required string Symptoms { get; init; }
    public required string Resolution { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
