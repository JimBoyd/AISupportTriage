using AISupportTriage.Models.Enums;

namespace AISupportTriage.Models.Responses;

public sealed record TicketResponse
{
    public int Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public TicketStatus Status { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public DateTime? ClosedAtUtc { get; init; }
    public List<TriageResultResponse> TriageResults { get; init; } = [];
    public List<StatusHistoryResponse> StatusHistory { get; init; } = [];
}

public sealed record TriageResultResponse
{
    public int Id { get; init; }
    public TicketCategory Category { get; init; }
    public TicketSeverity Severity { get; init; }
    public required string Summary { get; init; }
    public required string LikelyCause { get; init; }
    public KnownIssueSummaryResponse? KnownIssue { get; init; }
    public List<string> RecommendedActions { get; init; } = [];
    public DateTime AnalyzedAtUtc { get; init; }
}

public sealed record KnownIssueSummaryResponse
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Title { get; init; }
}

public sealed record StatusHistoryResponse
{
    public TicketStatus? PreviousStatus { get; init; }
    public TicketStatus NewStatus { get; init; }
    public DateTime ChangedAtUtc { get; init; }
    public string? Notes { get; init; }
}
