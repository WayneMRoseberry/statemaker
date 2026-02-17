namespace StateMaker;

public interface IExpressionEvaluator
{
    bool EvaluateBoolean(string expression, Dictionary<string, object> variables);
    object Evaluate(string expression, Dictionary<string, object> variables);
}
