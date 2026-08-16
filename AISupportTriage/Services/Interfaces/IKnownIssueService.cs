using AISupportTriage.Models.Entities;
using AISupportTriage.Models.Enums;

namespace AISupportTriage.Services.Interfaces;

public interface IKnownIssueService
{
    Task<List<KnownIssue>> SearchKnownIssuesAsync(string? category, string? symptoms, CancellationToken cancellationToken = default);
    Task<KnownIssue?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<KnownIssue?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<KnownIssue>> GetAllAsync(TicketCategory? category = null, bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<KnownIssue> CreateAsync(KnownIssue knownIssue, CancellationToken cancellationToken = default);
    Task<KnownIssue> UpdateAsync(KnownIssue knownIssue, CancellationToken cancellationToken = default);
    Task DisableAsync(int id, CancellationToken cancellationToken = default);
}
