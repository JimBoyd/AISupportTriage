using AISupportTriage.Models.Enums;

namespace AISupportTriage.AI.Models;

public sealed record AiTriageResponse
{
    public TicketCategory Category { get; init; }
    public TicketSeverity Severity { get; init; }
    public required string Summary { get; init; }
    public required string LikelyCause { get; init; }
    public List<string> RecommendedActions { get; init; } = [];
    public string? MatchedKnownIssueCode { get; init; }
}
