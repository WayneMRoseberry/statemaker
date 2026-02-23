using System.Text.Json;

namespace StateMaker;

public class FilterCommand
{
    private readonly JsonImporter _importer = new();

    public void Execute(string stateMachineFilePath, string filterFilePath, string? outputPath, string format, TextWriter writer, bool list = false)
    {
        if (!File.Exists(stateMachineFilePath))
            throw new FileNotFoundException($"State machine file not found: {stateMachineFilePath}", stateMachineFilePath);

        if (!File.Exists(filterFilePath))
            throw new FileNotFoundException($"Filter definition file not found: {filterFilePath}", filterFilePath);

        var smJson = File.ReadAllText(stateMachineFilePath);
        var stateMachine = _importer.Import(smJson);

        var filterDefinition = FilterDefinitionLoader.LoadFromFile(filterFilePath);

        var filterEngine = new FilterEngine(new ExpressionEvaluator());
        var filterResult = filterEngine.Apply(stateMachine, filterDefinition);

        if (list)
        {
            var output = SerializeMatchingStates(filterResult);
            if (outputPath is not null)
                File.WriteAllText(outputPath, output);
            else
                writer.Write(output);
        }
        else
        {
            var pathFilter = new PathFilter(filterResult.StateMachine, filterResult.SelectedStateIds);
            var filteredMachine = pathFilter.Filter();

            var exporter = ExporterFactory.GetExporter(format);
            var output = exporter.Export(filteredMachine);

            if (outputPath is not null)
                File.WriteAllText(outputPath, output);
            else
                writer.Write(output);
        }
    }

    private static string SerializeMatchingStates(FilterResult filterResult)
    {
        using var stream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        jsonWriter.WriteStartArray();

        foreach (var stateId in filterResult.SelectedStateIds)
        {
            if (!filterResult.StateMachine.States.TryGetValue(stateId, out var state))
                continue;

            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("stateId", stateId);

            jsonWriter.WriteStartObject("variables");
            foreach (var kvp in state.Variables)
            {
                WriteJsonValue(jsonWriter, kvp.Key, kvp.Value);
            }
            jsonWriter.WriteEndObject();

            jsonWriter.WriteStartObject("attributes");
            foreach (var kvp in state.Attributes)
            {
                WriteJsonValue(jsonWriter, kvp.Key, kvp.Value);
            }
            jsonWriter.WriteEndObject();

            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndArray();
        jsonWriter.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteJsonValue(Utf8JsonWriter writer, string propertyName, object? value)
    {
        switch (value)
        {
            case string s:
                writer.WriteString(propertyName, s);
                break;
            case int i:
                writer.WriteNumber(propertyName, i);
                break;
            case double d:
                writer.WriteNumber(propertyName, d);
                break;
            case bool b:
                writer.WriteBoolean(propertyName, b);
                break;
            case null:
                writer.WriteNull(propertyName);
                break;
            default:
                writer.WriteString(propertyName, value.ToString());
                break;
        }
    }
}
