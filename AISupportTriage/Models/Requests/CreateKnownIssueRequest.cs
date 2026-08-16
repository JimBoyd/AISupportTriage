using System.ComponentModel.DataAnnotations;
using AISupportTriage.Models.Enums;

namespace AISupportTriage.Models.Requests;

public sealed record CreateKnownIssueRequest
{
    [Required]
    [StringLength(50)]
    public required string Code { get; init; }

    [Required]
    [StringLength(200)]
    public required string Title { get; init; }

    [Required]
    [StringLength(2000)]
    public required string Description { get; init; }

    [Required]
    public TicketCategory Category { get; init; }

    [Required]
    [StringLength(1000)]
    public required string Symptoms { get; init; }

    [Required]
    [StringLength(2000)]
    public required string Resolution { get; init; }
}
