namespace StateMaker;

public static class ExporterFactory
{
    public static IStateMachineExporter GetExporter(string format)
    {
        ArgumentNullException.ThrowIfNull(format);

        return format.ToUpperInvariant() switch
        {
            "JSON" => new JsonExporter(),
            "DOT" => new DotExporter(),
            "GRAPHML" => new GraphMlExporter(),
            _ => throw new ArgumentException(
                $"Unsupported export format '{format}'. Supported formats: json, dot, graphml.", nameof(format))
        };
    }
}
