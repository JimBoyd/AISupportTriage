using System.ComponentModel.DataAnnotations;

namespace AISupportTriage.Models.Requests;

public sealed record ReopenTicketRequest
{
    [StringLength(500)]
    public string? Notes { get; init; }
}
