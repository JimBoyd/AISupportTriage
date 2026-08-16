using Microsoft.Extensions.AI;
using AISupportTriage.Data;
using AISupportTriage.Models.Entities;
using AISupportTriage.Models.Enums;
using AISupportTriage.Services.Interfaces;
using AISupportTriage.AI;
using AISupportTriage.AI.Models;
using AISupportTriage.Observability;

namespace AISupportTriage.Services;

public class TicketTriageService : ITicketTriageService
{
    private readonly AppDbContext _context;
    private readonly IChatClient _chatClient;
    private readonly IKnownIssueService _knownIssueService;
    private readonly ITicketService _ticketService;
    private readonly ILogger<TicketTriageService> _logger;

    public TicketTriageService(
        AppDbContext context,
        IChatClient chatClient,
        IKnownIssueService knownIssueService,
        ITicketService ticketService,
        ILogger<TicketTriageService> logger)
    {
        _context = context;
        _chatClient = chatClient;
        _knownIssueService = knownIssueService;
        _ticketService = ticketService;
        _logger = logger;
    }

    public async Task<TriageResult> TriageTicketAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        using var activity = Telemetry.StartActivity("support-ticket.triage");
        activity?.SetTag("ticket.id", ticketId);

        _logger.LogInformation("Starting AI triage for ticket {TicketId}", ticketId);

        // Load the ticket
        var ticket = await _ticketService.GetByIdAsync(ticketId, false, cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {ticketId} not found");
        }

        // Build the AI messages
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SupportTriagePrompt.SystemPrompt),
            new(ChatRole.User, $"""
                Analyze this support ticket:

                Title: {ticket.Title}
                Description: {ticket.Description}

                Provide a structured triage analysis including category, severity, summary, likely cause, and recommended actions.
                """)
        };

        // Call the AI with structured output - request JSON format in the prompt
        AiTriageResponse aiResponse;
        try
        {
            // Add instruction for JSON output
            messages.Add(new ChatMessage(ChatRole.User,
                "Respond ONLY with a valid JSON object matching this structure: " +
                "{ \"Category\": \"<TicketCategory>\", \"Severity\": \"<TicketSeverity>\", " +
                "\"Summary\": \"<string>\", \"LikelyCause\": \"<string>\", " +
                "\"RecommendedActions\": [\"<string>\"], \"MatchedKnownIssueCode\": \"<string or null>\" }"));

            var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);

            if (response == null || string.IsNullOrWhiteSpace(response.Text))
            {
                throw new InvalidOperationException("AI triage returned null response");
            }

            // Parse the JSON response
            var jsonText = response.Text.Trim();

            // Clean up potential markdown code blocks
            if (jsonText.StartsWith("```json"))
            {
                jsonText = jsonText.Substring(7);
            }
            if (jsonText.StartsWith("```"))
            {
                jsonText = jsonText.Substring(3);
            }
            if (jsonText.EndsWith("```"))
            {
                jsonText = jsonText.Substring(0, jsonText.Length - 3);
            }
            jsonText = jsonText.Trim();

            aiResponse = System.Text.Json.JsonSerializer.Deserialize<AiTriageResponse>(jsonText)
                ?? throw new InvalidOperationException("Failed to deserialize AI response");

            _logger.LogInformation("AI triage completed for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI triage failed for ticket {TicketId}", ticketId);
            throw new InvalidOperationException("AI triage service unavailable", ex);
        }

        // Persist the triage result
        using var persistActivity = Telemetry.StartActivity("triage.persist");

        var triageResult = new TriageResult
        {
            SupportTicketId = ticketId,
            Category = aiResponse.Category,
            Severity = aiResponse.Severity,
            Summary = aiResponse.Summary,
            LikelyCause = aiResponse.LikelyCause,
            AnalyzedAtUtc = DateTime.UtcNow
        };

        // Match the known issue if one was returned
        if (!string.IsNullOrWhiteSpace(aiResponse.MatchedKnownIssueCode))
        {
            var knownIssue = await _knownIssueService.GetByCodeAsync(
                aiResponse.MatchedKnownIssueCode,
                cancellationToken);

            if (knownIssue != null)
            {
                triageResult.KnownIssueId = knownIssue.Id;
                _logger.LogInformation("Matched known issue {KnownIssueCode} for ticket {TicketId}",
                    knownIssue.Code, ticketId);
            }
            else
            {
                _logger.LogWarning("AI suggested known issue code {Code} but it was not found in database",
                    aiResponse.MatchedKnownIssueCode);
            }
        }

        _context.TriageResults.Add(triageResult);
        await _context.SaveChangesAsync(cancellationToken);

        // Persist recommendations
        if (aiResponse.RecommendedActions.Count > 0)
        {
            var recommendations = aiResponse.RecommendedActions
                .Select((action, index) => new TriageRecommendation
                {
                    TriageResultId = triageResult.Id,
                    Sequence = index + 1,
                    Description = action
                })
                .ToList();

            _context.TriageRecommendations.AddRange(recommendations);
            await _context.SaveChangesAsync(cancellationToken);

            triageResult.Recommendations = recommendations;
        }

        _logger.LogInformation("Stored triage result {TriageResultId} for ticket {TicketId}",
            triageResult.Id, ticketId);

        // Update ticket status if it's Open
        if (ticket.Status == TicketStatus.Open)
        {
            using var statusActivity = Telemetry.StartActivity("ticket.status-change");

            await _ticketService.ChangeStatusAsync(
                ticketId,
                TicketStatus.Triaged,
                "AI triage completed.",
                cancellationToken);
        }

        return triageResult;
    }
}
