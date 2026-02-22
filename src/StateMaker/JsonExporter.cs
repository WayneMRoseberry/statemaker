using System.Text.Json;

namespace StateMaker;

public class JsonExporter : IStateMachineExporter
{
    public string Export(StateMachine stateMachine)
    {
        ArgumentNullException.ThrowIfNull(stateMachine);

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
