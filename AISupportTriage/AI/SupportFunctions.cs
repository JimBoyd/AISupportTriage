using System.ComponentModel;
using AISupportTriage.Services.Interfaces;

namespace AISupportTriage.AI;

public class SupportFunctions
{
    private readonly IKnownIssueService _knownIssueService;
    private readonly ILogger<SupportFunctions> _logger;

    public SupportFunctions(IKnownIssueService knownIssueService, ILogger<SupportFunctions> logger)
    {
        _knownIssueService = knownIssueService;
        _logger = logger;
    }

    [Description("Search the known issues database for issues matching a specific category and symptoms. Returns relevant known issues that may match the reported problem.")]
    public async Task<string> SearchKnownIssues(
        [Description("The category of the issue (e.g., Deployment, Database, Authentication)")] string category,
        [Description("The symptoms or keywords to search for")] string symptoms)
    {
        _logger.LogInformation("AI function invoked: SearchKnownIssues - Category: {Category}, Symptoms: {Symptoms}", category, symptoms);

        var results = await _knownIssueService.SearchKnownIssuesAsync(category, symptoms);

        if (results.Count == 0)
        {
            return "No matching known issues found.";
        }

        var formattedResults = results.Select(k => new
        {
            code = k.Code,
            title = k.Title,
            category = k.Category.ToString(),
            symptoms = k.Symptoms,
            resolution = k.Resolution
        });

        return System.Text.Json.JsonSerializer.Serialize(formattedResults, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    [Description("Get detailed information about a specific known issue by its code (e.g., ORD-1042).")]
    public async Task<string> GetKnownIssueDetails(
        [Description("The unique code of the known issue (e.g., ORD-1042)")] string code)
    {
        _logger.LogInformation("AI function invoked: GetKnownIssueDetails - Code: {Code}", code);

        var issue = await _knownIssueService.GetByCodeAsync(code);

        if (issue == null)
        {
            return $"Known issue with code '{code}' not found.";
        }

        var result = new
        {
            code = issue.Code,
            title = issue.Title,
            description = issue.Description,
            category = issue.Category.ToString(),
            symptoms = issue.Symptoms,
            resolution = issue.Resolution
        };

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
