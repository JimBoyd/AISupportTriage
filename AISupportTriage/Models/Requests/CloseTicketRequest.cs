using System.ComponentModel.DataAnnotations;

namespace AISupportTriage.Models.Requests;

public sealed record CloseTicketRequest
{
    [StringLength(500)]
    public string? Notes { get; init; }
}
