namespace StateMaker;

public class DeclarativeRule : IRule
{
    private readonly string _name;
    private readonly string _condition;
    private readonly Dictionary<string, string> _transformations;
    private readonly IExpressionEvaluator _evaluator;

    public DeclarativeRule(
        string name,
        string condition,
        Dictionary<string, string> transformations,
        IExpressionEvaluator evaluator)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(transformations);
        ArgumentNullException.ThrowIfNull(evaluator);

        _name = name;
        _condition = condition;
        _transformations = transformations;
        _evaluator = evaluator;
    }

    public bool IsAvailable(State state)
    {
        var variables = GetNonNullableVariables(state);
        return _evaluator.EvaluateBooleanLenient(_condition, variables);
    }

    public State Execute(State state)
    {
        var clone = state.Clone();
        var originalVariables = GetNonNullableVariables(state);

        foreach (var kvp in _transformations)
        {
            var value = _evaluator.EvaluateLenient(kvp.Value, originalVariables);
            clone.Variables[kvp.Key] = value;
        }

        return clone;
    }

    public string GetName() => _name;

    private static Dictionary<string, object> GetNonNullableVariables(State state)
    {
        var dict = new Dictionary<string, object>();
        foreach (var kvp in state.Variables)
        {
            if (kvp.Value is not null)
                dict[kvp.Key] = kvp.Value;
        }
        return dict;
    }
}
