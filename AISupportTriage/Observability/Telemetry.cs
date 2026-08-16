using System.Diagnostics;

namespace AISupportTriage.Observability;

public static class Telemetry
{
    public const string SourceName = "AISupportTriage";

    private static readonly ActivitySource ActivitySource = new(SourceName);

    public static Activity? StartActivity(string name)
    {
        return ActivitySource.StartActivity(name);
    }
}
