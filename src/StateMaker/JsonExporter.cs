using System.Text.Json;

namespace StateMaker;

public class JsonExporter : IStateMachineExporter
{
    public string Export(StateMachine stateMachine, ExportType exportType = ExportType.Default, bool includeStateVariables = false)
    {
        ArgumentNullException.ThrowIfNull(stateMachine);

        if (exportType != ExportType.Default)
            return ExportTraversals(stateMachine, exportType, includeStateVariables);

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        writer.WriteString(RulesJsonPropertyNames.StartingStateId, stateMachine.StartingStateId);

        writer.WriteStartObject(RulesJsonPropertyNames.States);
        foreach (var kvp in stateMachine.States)
        {
            writer.WriteStartObject(kvp.Key);
            foreach (var variable in kvp.Value.Variables)
            {
                WriteJsonValue(writer, variable.Key, variable.Value);
            }
            if (kvp.Value.Attributes.Count > 0)
            {
                writer.WriteStartObject("attributes");
                foreach (var attr in kvp.Value.Attributes)
                {
                    WriteJsonValue(writer, attr.Key, attr.Value);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();

        writer.WriteStartArray(RulesJsonPropertyNames.Transitions);
        foreach (var transition in stateMachine.Transitions)
        {
            writer.WriteStartObject();
            writer.WriteString(RulesJsonPropertyNames.SourceStateId, transition.SourceStateId);
            writer.WriteString(RulesJsonPropertyNames.TargetStateId, transition.TargetStateId);
            writer.WriteString(RulesJsonPropertyNames.RuleName, transition.RuleName);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string ExportTraversals(StateMachine stateMachine, ExportType exportType, bool includeStateVariables)
    {
        var traversals = TraversalGenerator.Generate(stateMachine, exportType);

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteStartArray("traversals");

        foreach (var traversal in traversals)
        {
            writer.WriteStartObject();
            writer.WriteString("id", traversal.Id);
            writer.WriteString("name", $"{traversal.Transitions.Count} steps - {traversal.Name}");
            writer.WriteString("description", traversal.Description);

            writer.WriteStartArray(RulesJsonPropertyNames.Transitions);
            foreach (var t in traversal.Transitions)
            {
                writer.WriteStartObject();
                writer.WriteString(RulesJsonPropertyNames.SourceStateId, t.SourceStateId);
                writer.WriteString(RulesJsonPropertyNames.TargetStateId, t.TargetStateId);
                writer.WriteString(RulesJsonPropertyNames.RuleName, t.RuleName);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            if (includeStateVariables)
            {
                var stateIds = GetTraversalStateIds(traversal, stateMachine.StartingStateId ?? string.Empty);
                writer.WriteStartObject("states");
                foreach (var stateId in stateIds)
                {
                    if (!stateMachine.States.TryGetValue(stateId, out var state)) continue;
                    writer.WriteStartObject(stateId);
                    foreach (var kvp in state.Variables)
                        WriteJsonValue(writer, kvp.Key, kvp.Value);
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static IEnumerable<string> GetTraversalStateIds(Traversal traversal, string startStateId)
    {
        if (traversal.Transitions.Count == 0)
            return new[] { startStateId };

        var seen = new HashSet<string>();
        var result = new List<string>();
        foreach (var t in traversal.Transitions)
        {
            if (seen.Add(t.SourceStateId)) result.Add(t.SourceStateId);
            if (seen.Add(t.TargetStateId)) result.Add(t.TargetStateId);
        }
        return result;
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
