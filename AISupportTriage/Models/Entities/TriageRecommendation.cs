namespace AISupportTriage.Models.Entities;

public class TriageRecommendation
{
    public int Id { get; set; }
    public int TriageResultId { get; set; }
    public int Sequence { get; set; }
    public required string Description { get; set; }

    public TriageResult? TriageResult { get; set; }
}
