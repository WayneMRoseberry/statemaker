using System.Text.Json;

namespace StateMaker;

public static class FilterDefinitionLoader
{
    public static FilterDefinition LoadFromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new JsonParseException(ex);
        }

        var root = doc.RootElement;

        if (!root.TryGetProperty("filters", out var filtersElement))
            throw new InvalidOperationException("JSON must contain a 'filters' array.");

        var definition = new FilterDefinition();

        foreach (var filterElement in filtersElement.EnumerateArray())
        {
            var rule = new FilterRule();

            if (!filterElement.TryGetProperty("condition", out var conditionElement)
                || conditionElement.GetString() is not { } condition)
            {
                throw new InvalidOperationException("Each filter must contain a 'condition' field.");
            }

            rule.Condition = condition;

            if (filterElement.TryGetProperty("attributes", out var attrsElement)
                && attrsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var attr in attrsElement.EnumerateObject())
                {
                    rule.Attributes[attr.Name] = ConvertJsonValue(attr.Value);
                }
            }

            definition.Filters.Add(rule);
        }

        return definition;
    }

    public static FilterDefinition LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Filter definition file not found: {filePath}", filePath);

        var json = File.ReadAllText(filePath);
        return LoadFromJson(json);
    }

    private static object? ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }
}
