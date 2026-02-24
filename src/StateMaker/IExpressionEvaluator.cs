namespace StateMaker;

public interface IExpressionEvaluator
{
    bool EvaluateBoolean(string expression, Dictionary<string, object> variables);
    object Evaluate(string expression, Dictionary<string, object> variables);

    /// <summary>
    /// Evaluates a boolean expression, treating undefined parameters as null
    /// instead of throwing. Used for rule conditions where some variables may
    /// not yet exist in the state.
    /// </summary>
    bool EvaluateBooleanLenient(string expression, Dictionary<string, object> variables);
}
