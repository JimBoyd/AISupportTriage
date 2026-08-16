using Microsoft.EntityFrameworkCore;
using AISupportTriage.Data;
using AISupportTriage.Models.Entities;
using AISupportTriage.Models.Enums;
using AISupportTriage.Services.Interfaces;

namespace AISupportTriage.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TicketService> _logger;

    public TicketService(AppDbContext context, ILogger<TicketService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SupportTicket> CreateAsync(string title, string description, CancellationToken cancellationToken = default)
    {
        var ticket = new SupportTicket
        {
            Title = title,
            Description = description,
            Status = TicketStatus.Open,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created ticket {TicketId}", ticket.Id);
        return ticket;
    }

    public async Task<SupportTicket?> GetByIdAsync(int id, bool includeHistory = false, CancellationToken cancellationToken = default)
    {
        var query = _context.SupportTickets.AsQueryable();

        if (includeHistory)
        {
            query = query
                .Include(t => t.TriageResults)
                    .ThenInclude(tr => tr.KnownIssue)
                .Include(t => t.TriageResults)
                    .ThenInclude(tr => tr.Recommendations)
                .Include(t => t.StatusHistory);
        }

        return await query.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<(List<SupportTicket> Items, int TotalCount)> GetAllAsync(
        TicketStatus? status = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SupportTickets.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<SupportTicket> UpdateAsync(int id, string title, string description, CancellationToken cancellationToken = default)
    {
        var ticket = await _context.SupportTickets.FindAsync([id], cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {id} not found");
        }

        ticket.Title = title;
        ticket.Description = description;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated ticket {TicketId}", id);
        return ticket;
    }

    public async Task<SupportTicket> ChangeStatusAsync(int id, TicketStatus newStatus, string? notes = null, CancellationToken cancellationToken = default)
    {
        var ticket = await _context.SupportTickets.FindAsync([id], cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {id} not found");
        }

        // If status hasn't changed, return without creating duplicate history
        if (ticket.Status == newStatus)
        {
            return ticket;
        }

        var previousStatus = ticket.Status;
        ticket.Status = newStatus;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        // Create status history record
        var statusHistory = new TicketStatusHistory
        {
            SupportTicketId = id,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedAtUtc = DateTime.UtcNow,
            Notes = notes
        };

        _context.TicketStatusHistories.Add(statusHistory);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ticket {TicketId} status changed from {OldStatus} to {NewStatus}",
            id, previousStatus, newStatus);

        return ticket;
    }

    public async Task<SupportTicket> CloseAsync(int id, string? notes = null, CancellationToken cancellationToken = default)
    {
        var ticket = await _context.SupportTickets.FindAsync([id], cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {id} not found");
        }

        var previousStatus = ticket.Status;
        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAtUtc = DateTime.UtcNow;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        // Create status history record
        var statusHistory = new TicketStatusHistory
        {
            SupportTicketId = id,
            PreviousStatus = previousStatus,
            NewStatus = TicketStatus.Closed,
            ChangedAtUtc = DateTime.UtcNow,
            Notes = notes
        };

        _context.TicketStatusHistories.Add(statusHistory);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Closed ticket {TicketId}", id);
        return ticket;
    }

    public async Task<SupportTicket> ReopenAsync(int id, string? notes = null, CancellationToken cancellationToken = default)
    {
        var ticket = await _context.SupportTickets.FindAsync([id], cancellationToken);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {id} not found");
        }

        var previousStatus = ticket.Status;
        ticket.Status = TicketStatus.Open;
        ticket.ClosedAtUtc = null;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        // Create status history record
        var statusHistory = new TicketStatusHistory
        {
            SupportTicketId = id,
            PreviousStatus = previousStatus,
            NewStatus = TicketStatus.Open,
            ChangedAtUtc = DateTime.UtcNow,
            Notes = notes
        };

        _context.TicketStatusHistories.Add(statusHistory);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reopened ticket {TicketId}", id);
        return ticket;
    }
}
