using System.Text.Json;

namespace StateMaker;

public class JsonParseException : Exception
{
    public long? LineNumber { get; }
    public long? Position { get; }

    public JsonParseException(JsonException innerException)
        : base(FormatMessage(innerException), innerException)
    {
        LineNumber = innerException.LineNumber.HasValue ? innerException.LineNumber.Value + 1 : null;
        Position = innerException.BytePositionInLine;
    }

    private static string FormatMessage(JsonException ex)
    {
        var description = ex.Message;

        // Strip internal System.Text.Json guidance like "Change the reader options."
        var changeIndex = description.IndexOf("Change the reader options.", StringComparison.OrdinalIgnoreCase);
        if (changeIndex > 0)
        {
            description = description[..changeIndex].TrimEnd();
        }

        // Strip the appended "LineNumber: X | BytePositionInLine: Y." since
        // we reconstruct location from the structured properties
        var lineNumberIndex = description.IndexOf("LineNumber:", StringComparison.OrdinalIgnoreCase);
        if (lineNumberIndex > 0)
        {
            description = description[..lineNumberIndex].TrimEnd();
        }

        if (ex.LineNumber.HasValue)
        {
            return $"Invalid JSON at line {ex.LineNumber.Value + 1}, position {ex.BytePositionInLine}: {description}";
        }

        return $"Invalid JSON: {description}";
    }
}
