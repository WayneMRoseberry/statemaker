namespace StateMaker;

public class FilterResult
{
    public HashSet<string> SelectedStateIds { get; } = new();
    public StateMachine StateMachine { get; set; } = new();
}

public class FilterEngine
{
    public const string StateIdVariableName = "_stateId";

    private readonly IExpressionEvaluator _evaluator;

    public FilterEngine(IExpressionEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public FilterResult Apply(StateMachine stateMachine, FilterDefinition filterDefinition)
    {
        var result = new FilterResult { StateMachine = stateMachine };

        foreach (var kvp in stateMachine.States)
        {
            var stateId = kvp.Key;
            var state = kvp.Value;
            var variables = GetNonNullableVariables(state);
            variables[StateIdVariableName] = stateId;

            foreach (var rule in filterDefinition.Filters)
            {
                if (_evaluator.EvaluateBoolean(rule.Condition, variables))
                {
                    result.SelectedStateIds.Add(stateId);

                    foreach (var attr in rule.Attributes)
                    {
                        state.Attributes[attr.Key] = attr.Value;
                    }
                }
            }
        }

        return result;
    }

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
