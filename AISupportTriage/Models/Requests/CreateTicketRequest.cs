using System.ComponentModel.DataAnnotations;

namespace AISupportTriage.Models.Requests;

public sealed record CreateTicketRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public required string Title { get; init; }

    [Required]
    [StringLength(5000, MinimumLength = 10)]
    public required string Description { get; init; }
}
