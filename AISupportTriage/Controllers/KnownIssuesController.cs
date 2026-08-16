using Microsoft.AspNetCore.Mvc;
using AISupportTriage.Models.Entities;
using AISupportTriage.Models.Enums;
using AISupportTriage.Models.Requests;
using AISupportTriage.Models.Responses;
using AISupportTriage.Services.Interfaces;

namespace AISupportTriage.Controllers;

[ApiController]
[Route("api/known-issues")]
public class KnownIssuesController : ControllerBase
{
    private readonly IKnownIssueService _knownIssueService;
    private readonly ILogger<KnownIssuesController> _logger;

    public KnownIssuesController(IKnownIssueService knownIssueService, ILogger<KnownIssuesController> logger)
    {
        _knownIssueService = knownIssueService;
        _logger = logger;
    }

    /// <summary>
    /// List all known issues with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<KnownIssueResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<KnownIssueResponse>>> List(
        [FromQuery] TicketCategory? category = null,
        [FromQuery] bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var issues = await _knownIssueService.GetAllAsync(category, activeOnly, cancellationToken);

            var response = issues.Select(i => new KnownIssueResponse
            {
                Id = i.Id,
                Code = i.Code,
                Title = i.Title,
                Description = i.Description,
                Category = i.Category,
                Symptoms = i.Symptoms,
                Resolution = i.Resolution,
                IsActive = i.IsActive,
                CreatedAtUtc = i.CreatedAtUtc,
                UpdatedAtUtc = i.UpdatedAtUtc
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing known issues");
            return Problem(
                title: "Error listing known issues",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get a known issue by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(KnownIssueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KnownIssueResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var issue = await _knownIssueService.GetByIdAsync(id, cancellationToken);

            if (issue == null)
            {
                return Problem(
                    title: "Known issue not found",
                    detail: $"Known issue {id} does not exist.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var response = new KnownIssueResponse
            {
                Id = issue.Id,
                Code = issue.Code,
                Title = issue.Title,
                Description = issue.Description,
                Category = issue.Category,
                Symptoms = issue.Symptoms,
                Resolution = issue.Resolution,
                IsActive = issue.IsActive,
                CreatedAtUtc = issue.CreatedAtUtc,
                UpdatedAtUtc = issue.UpdatedAtUtc
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving known issue {IssueId}", id);
            return Problem(
                title: "Error retrieving known issue",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Create a new known issue
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(KnownIssueResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<KnownIssueResponse>> Create(
        [FromBody] CreateKnownIssueRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var issue = new KnownIssue
            {
                Code = request.Code,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Symptoms = request.Symptoms,
                Resolution = request.Resolution
            };

            var created = await _knownIssueService.CreateAsync(issue, cancellationToken);

            var response = new KnownIssueResponse
            {
                Id = created.Id,
                Code = created.Code,
                Title = created.Title,
                Description = created.Description,
                Category = created.Category,
                Symptoms = created.Symptoms,
                Resolution = created.Resolution,
                IsActive = created.IsActive,
                CreatedAtUtc = created.CreatedAtUtc,
                UpdatedAtUtc = created.UpdatedAtUtc
            };

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating known issue");
            return Problem(
                title: "Error creating known issue",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Update an existing known issue
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(KnownIssueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KnownIssueResponse>> Update(
        int id,
        [FromBody] UpdateKnownIssueRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _knownIssueService.GetByIdAsync(id, cancellationToken);
            if (existing == null)
            {
                return Problem(
                    title: "Known issue not found",
                    detail: $"Known issue {id} does not exist.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            existing.Code = request.Code;
            existing.Title = request.Title;
            existing.Description = request.Description;
            existing.Category = request.Category;
            existing.Symptoms = request.Symptoms;
            existing.Resolution = request.Resolution;

            var updated = await _knownIssueService.UpdateAsync(existing, cancellationToken);

            var response = new KnownIssueResponse
            {
                Id = updated.Id,
                Code = updated.Code,
                Title = updated.Title,
                Description = updated.Description,
                Category = updated.Category,
                Symptoms = updated.Symptoms,
                Resolution = updated.Resolution,
                IsActive = updated.IsActive,
                CreatedAtUtc = updated.CreatedAtUtc,
                UpdatedAtUtc = updated.UpdatedAtUtc
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating known issue {IssueId}", id);
            return Problem(
                title: "Error updating known issue",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Delete (soft delete) a known issue
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _knownIssueService.GetByIdAsync(id, cancellationToken);
            if (existing == null)
            {
                return Problem(
                    title: "Known issue not found",
                    detail: $"Known issue {id} does not exist.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            await _knownIssueService.DisableAsync(id, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting known issue {IssueId}", id);
            return Problem(
                title: "Error deleting known issue",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
