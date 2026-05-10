namespace StateMaker;

public enum ExportType
{
    Default,
    AllStates,
    AllTransitions,
    AllPaths,
    AllStatePairs
}

public static class ExportTypeParser
{
    public static ExportType Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ExportType.Default;

        if (Enum.TryParse<ExportType>(value, ignoreCase: true, out var exportType))
            return exportType;

        throw new ArgumentException(
            $"Unsupported export type '{value}'. Supported values: {string.Join(", ", Enum.GetNames<ExportType>())}.",
            nameof(value));
    }
}
