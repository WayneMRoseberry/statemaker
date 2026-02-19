using System.Text.Json;

namespace StateMaker;

public class JsonImporter : IStateMachineImporter
{
    public StateMachine Import(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(content);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON syntax: {ex.Message}", ex);
        }

        var root = doc.RootElement;
        var stateMachine = new StateMachine();

        if (!root.TryGetProperty(RulesJsonPropertyNames.States, out var statesElement))
            throw new InvalidOperationException($"JSON must contain a '{RulesJsonPropertyNames.States}' object.");

        foreach (var stateProperty in statesElement.EnumerateObject())
        {
            var state = new State();
            foreach (var variable in stateProperty.Value.EnumerateObject())
            {
                state.Variables[variable.Name] = ConvertJsonValue(variable.Value);
            }
            stateMachine.AddOrUpdateState(stateProperty.Name, state);
        }

        if (root.TryGetProperty(RulesJsonPropertyNames.StartingStateId, out var startingElement)
            && startingElement.ValueKind == JsonValueKind.String)
        {
            stateMachine.StartingStateId = startingElement.GetString();
        }

        if (root.TryGetProperty(RulesJsonPropertyNames.Transitions, out var transitionsElement))
        {
            foreach (var transElement in transitionsElement.EnumerateArray())
            {
                var sourceStateId = transElement.GetProperty(RulesJsonPropertyNames.SourceStateId).GetString()
                    ?? throw new InvalidOperationException($"Transition missing '{RulesJsonPropertyNames.SourceStateId}'.");
                var targetStateId = transElement.GetProperty(RulesJsonPropertyNames.TargetStateId).GetString()
                    ?? throw new InvalidOperationException($"Transition missing '{RulesJsonPropertyNames.TargetStateId}'.");
                var ruleName = transElement.GetProperty(RulesJsonPropertyNames.RuleName).GetString()
                    ?? throw new InvalidOperationException($"Transition missing '{RulesJsonPropertyNames.RuleName}'.");

                stateMachine.Transitions.Add(new Transition(sourceStateId, targetStateId, ruleName));
            }
        }

        return stateMachine;
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
