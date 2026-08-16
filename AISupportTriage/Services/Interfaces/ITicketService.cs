using AISupportTriage.Models.Entities;
using AISupportTriage.Models.Enums;

namespace AISupportTriage.Services.Interfaces;

public interface ITicketService
{
    Task<SupportTicket> CreateAsync(string title, string description, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByIdAsync(int id, bool includeHistory = false, CancellationToken cancellationToken = default);
    Task<(List<SupportTicket> Items, int TotalCount)> GetAllAsync(TicketStatus? status = null, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default);
    Task<SupportTicket> UpdateAsync(int id, string title, string description, CancellationToken cancellationToken = default);
    Task<SupportTicket> ChangeStatusAsync(int id, TicketStatus newStatus, string? notes = null, CancellationToken cancellationToken = default);
    Task<SupportTicket> CloseAsync(int id, string? notes = null, CancellationToken cancellationToken = default);
    Task<SupportTicket> ReopenAsync(int id, string? notes = null, CancellationToken cancellationToken = default);
}
