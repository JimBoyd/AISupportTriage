namespace AISupportTriage.AI;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ModelName { get; set; } = "gemma4:26b";
    public float Temperature { get; set; } = 0.2f;
}
