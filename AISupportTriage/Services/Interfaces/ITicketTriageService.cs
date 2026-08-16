using AISupportTriage.Models.Entities;

namespace AISupportTriage.Services.Interfaces;

public interface ITicketTriageService
{
    Task<TriageResult> TriageTicketAsync(int ticketId, CancellationToken cancellationToken = default);
}
