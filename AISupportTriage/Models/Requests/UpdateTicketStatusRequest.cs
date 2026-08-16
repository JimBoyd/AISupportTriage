using System.ComponentModel.DataAnnotations;
using AISupportTriage.Models.Enums;

namespace AISupportTriage.Models.Requests;

public sealed record UpdateTicketStatusRequest
{
    [Required]
    public TicketStatus Status { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}
