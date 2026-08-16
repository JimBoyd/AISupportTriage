using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AISupportTriage.Models.Enums;
using AISupportTriage.Models.Requests;
using AISupportTriage.Models.Responses;
using AISupportTriage.Services.Interfaces;

namespace AISupportTriage.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ITicketTriageService _triageService;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        ITicketService ticketService,
        ITicketTriageService triageService,
        ILogger<TicketsController> logger)
    {
        _ticketService = ticketService;
        _triageService = triageService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new support ticket
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketResponse>> Create(
        [FromBody] CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _ticketService.CreateAsync(
                request.Title,
                request.Description,
                cancellationToken);

            var response = new TicketResponse
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status,
                CreatedAtUtc = ticket.CreatedAtUtc,
                UpdatedAtUtc = ticket.UpdatedAtUtc,
                ClosedAtUtc = ticket.ClosedAtUtc
            };

            return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket");
            return Problem(
                title: "Error creating ticket",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get a ticket by ID with full details including triage history
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _ticketService.GetByIdAsync(id, includeHistory: true, cancellationToken);

            if (ticket == null)
            {
                return Problem(
                    title: "Ticket not found",
                    detail: $"Support ticket {id} does not exist.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var response = new TicketResponse
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status,
                CreatedAtUtc = ticket.CreatedAtUtc,
                UpdatedAtUtc = ticket.UpdatedAtUtc,
                ClosedAtUtc = ticket.ClosedAtUtc,
                TriageResults = ticket.TriageResults.Select(tr => new TriageResultResponse
                {
                    Id = tr.Id,
                    Category = tr.Category,
                    Severity = tr.Severity,
                    Summary = tr.Summary,
                    LikelyCause = tr.LikelyCause,
                    KnownIssue = tr.KnownIssue != null ? new KnownIssueSummaryResponse
                    {
                        Id = tr.KnownIssue.Id,
                        Code = tr.KnownIssue.Code,
                        Title = tr.KnownIssue.Title
                    } : null,
                    RecommendedActions = tr.Recommendations
                        .OrderBy(r => r.Sequence)
                        .Select(r => r.Description)
                        .ToList(),
                    AnalyzedAtUtc = tr.AnalyzedAtUtc
                }).ToList(),
                StatusHistory = ticket.StatusHistory.Select(sh => new StatusHistoryResponse
                {
                    PreviousStatus = sh.PreviousStatus,
                    NewStatus = sh.NewStatus,
                    ChangedAtUtc = sh.ChangedAtUtc,
                    Notes = sh.Notes
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ticket {TicketId}", id);
            return Problem(
                title: "Error retrieving ticket",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// List tickets with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TicketListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TicketListItemResponse>>> List(
        [FromQuery] TicketStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (items, totalCount) = await _ticketService.GetAllAsync(status, page, pageSize, cancellationToken);

            var response = new PagedResponse<TicketListItemResponse>
            {
                Items = items.Select(t => new TicketListItemResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    CreatedAtUtc = t.CreatedAtUtc,
                    UpdatedAtUtc = t.UpdatedAtUtc
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing tickets");
            return Problem(
                title: "Error listing tickets",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Update ticket title and description
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponse>> Update(
        int id,
        [FromBody] UpdateTicketRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _ticketService.UpdateAsync(id, request.Title, request.Description, cancellationToken);

            var response = new TicketResponse
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status,
                CreatedAtUtc = ticket.CreatedAtUtc,
                UpdatedAtUtc = ticket.UpdatedAtUtc,
                ClosedAtUtc = ticket.ClosedAtUtc
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Ticket not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket {TicketId}", id);
            return Problem(
                title: "Error updating ticket",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Run AI triage on a ticket
    /// </summary>
    [HttpPost("{id}/triage")]
    [ProducesResponseType(typeof(TriageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<TriageResponse>> Triage(int id, CancellationToken cancellationToken)
    {
        try
        {
            var triageResult = await _triageService.TriageTicketAsync(id, cancellationToken);

            var response = new TriageResponse
            {
                TicketId = triageResult.SupportTicketId,
                TriageResultId = triageResult.Id,
                Category = triageResult.Category,
                Severity = triageResult.Severity,
                Summary = triageResult.Summary,
                LikelyCause = triageResult.LikelyCause,
                KnownIssue = triageResult.KnownIssue != null ? new KnownIssueDetailResponse
                {
                    Id = triageResult.KnownIssue.Id,
                    Code = triageResult.KnownIssue.Code,
                    Title = triageResult.KnownIssue.Title,
                    Resolution = triageResult.KnownIssue.Resolution
                } : null,
                RecommendedActions = triageResult.Recommendations
                    .OrderBy(r => r.Sequence)
                    .Select(r => r.Description)
                    .ToList(),
                AnalyzedAtUtc = triageResult.AnalyzedAtUtc
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Problem(
                title: "Ticket not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("unavailable"))
        {
            return Problem(
                title: "AI service unavailable",
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triaging ticket {TicketId}", id);
            return Problem(
                title: "Error during triage",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Change ticket status
    /// </summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponse>> ChangeStatus(
        int id,
        [FromBody] UpdateTicketStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _ticketService.ChangeStatusAsync(id, request.Status, request.Notes, cancellationToken);

            var response = new TicketResponse
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status,
                CreatedAtUtc = ticket.CreatedAtUtc,
                UpdatedAtUtc = ticket.UpdatedAtUtc,
                ClosedAtUtc = ticket.ClosedAtUtc
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Ticket not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing status for ticket {TicketId}", id);
            return Problem(
                title: "Error changing status",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Close a ticket
    /// </summary>
    [HttpPost("{id}/close")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponse>> Close(
        int id,
        [FromBody] CloseTicketRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _ticketService.CloseAsync(id, request?.Notes, cancellationToken);

            var response = new TicketResponse
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status,
                CreatedAtUtc = ticket.CreatedAtUtc,
                UpdatedAtUtc = ticket.UpdatedAtUtc,
                ClosedAtUtc = ticket.ClosedAtUtc
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Ticket not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing ticket {TicketId}", id);
            return Problem(
                title: "Error closing ticket",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Reopen a closed ticket
    /// </summary>
    [HttpPost("{id}/reopen")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponse>> Reopen(
        int id,
        [FromBody] ReopenTicketRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _ticketService.ReopenAsync(id, request?.Notes, cancellationToken);

            var response = new TicketResponse
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status,
                CreatedAtUtc = ticket.CreatedAtUtc,
                UpdatedAtUtc = ticket.UpdatedAtUtc,
                ClosedAtUtc = ticket.ClosedAtUtc
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Ticket not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reopening ticket {TicketId}", id);
            return Problem(
                title: "Error reopening ticket",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
