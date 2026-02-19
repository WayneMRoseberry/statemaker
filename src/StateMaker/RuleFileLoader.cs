using System.Reflection;
using System.Text.Json;

namespace StateMaker;

public class RuleFileLoader
{
    private readonly IExpressionEvaluator _evaluator;

    public RuleFileLoader(IExpressionEvaluator evaluator)
    {
        ArgumentNullException.ThrowIfNull(evaluator);
        _evaluator = evaluator;
    }

    public (State? initialState, IRule[] rules) LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Rule file not found: {filePath}", filePath);

        var json = File.ReadAllText(filePath);
        return LoadFromJson(json);
    }

    public (State? initialState, IRule[] rules) LoadFromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON syntax: {ex.Message}", ex);
        }

        var root = doc.RootElement;

        var initialState = ParseInitialState(root);
        var rules = ParseRules(root);

        return (initialState, rules);
    }

    private static State? ParseInitialState(JsonElement root)
    {
        if (!root.TryGetProperty("initialState", out var stateElement))
            return null;

        if (stateElement.ValueKind == JsonValueKind.Null)
            return null;

        var state = new State();
        foreach (var prop in stateElement.EnumerateObject())
        {
            state.Variables[prop.Name] = ConvertJsonValue(prop.Value);
        }
        return state;
    }

    private IRule[] ParseRules(JsonElement root)
    {
        if (!root.TryGetProperty("rules", out var rulesElement))
            throw new InvalidOperationException("JSON must contain a 'rules' array.");

        var rules = new List<IRule>();
        foreach (var ruleElement in rulesElement.EnumerateArray())
        {
            rules.Add(ParseSingleRule(ruleElement));
        }
        return rules.ToArray();
    }

    private IRule ParseSingleRule(JsonElement element)
    {
        var type = GetOptionalString(element, "type");

        if (type == null || type.Equals("declarative", StringComparison.OrdinalIgnoreCase))
            return ParseDeclarativeRule(element);

        if (type.Equals("custom", StringComparison.OrdinalIgnoreCase))
            return ParseCustomRule(element);

        throw new InvalidOperationException(
            $"Unknown rule type '{type}'. Supported types: 'declarative', 'custom'.");
    }

    private DeclarativeRule ParseDeclarativeRule(JsonElement element)
    {
        var name = GetOptionalString(element, "name")
            ?? throw new InvalidOperationException(
                "Declarative rule is missing required 'name' field.");

        var condition = GetOptionalString(element, "condition")
            ?? throw new InvalidOperationException(
                $"Declarative rule '{name}' is missing required 'condition' field.");

        var transformations = new Dictionary<string, string>();
        if (element.TryGetProperty("transformations", out var transElement))
        {
            foreach (var prop in transElement.EnumerateObject())
            {
                transformations[prop.Name] = prop.Value.GetString()
                    ?? throw new InvalidOperationException(
                        $"Transformation value for '{prop.Name}' in rule '{name}' must be a string expression.");
            }
        }

        return new DeclarativeRule(name, condition, transformations, _evaluator);
    }

    private static IRule ParseCustomRule(JsonElement element)
    {
        var assemblyPath = GetOptionalString(element, "assemblyPath")
            ?? throw new InvalidOperationException(
                "Custom rule is missing required 'assemblyPath' field.");

        var className = GetOptionalString(element, "className")
            ?? throw new InvalidOperationException(
                "Custom rule is missing required 'className' field.");

        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFrom(assemblyPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to load assembly '{assemblyPath}': {ex.Message}", ex);
        }

        var type = assembly.GetType(className)
            ?? throw new InvalidOperationException(
                $"Class '{className}' not found in assembly '{assemblyPath}'.");

        if (!typeof(IRule).IsAssignableFrom(type))
            throw new InvalidOperationException(
                $"Class '{className}' does not implement IRule.");

        var constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
            throw new InvalidOperationException(
                $"Class '{className}' does not have a parameterless constructor.");

        try
        {
            return (IRule)constructor.Invoke(null);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to instantiate class '{className}': {ex.Message}", ex);
        }
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

    private static string? GetOptionalString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;
    }
}
