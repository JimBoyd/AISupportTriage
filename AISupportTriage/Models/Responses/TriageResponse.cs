using AISupportTriage.Models.Enums;

namespace AISupportTriage.Models.Responses;

public sealed record TriageResponse
{
    public int TicketId { get; init; }
    public int TriageResultId { get; init; }
    public TicketCategory Category { get; init; }
    public TicketSeverity Severity { get; init; }
    public required string Summary { get; init; }
    public required string LikelyCause { get; init; }
    public KnownIssueDetailResponse? KnownIssue { get; init; }
    public List<string> RecommendedActions { get; init; } = [];
    public DateTime AnalyzedAtUtc { get; init; }
}

public sealed record KnownIssueDetailResponse
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Title { get; init; }
    public required string Resolution { get; init; }
}
