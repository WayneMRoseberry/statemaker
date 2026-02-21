namespace StateMaker;

public class ExpressionEvaluationException : Exception
{
    public string Expression { get; }

    public ExpressionEvaluationException(string expression, string reason)
        : base($"Error in expression '{expression}': {reason}")
    {
        Expression = expression;
    }

    public ExpressionEvaluationException(string expression, string reason, Exception innerException)
        : base($"Error in expression '{expression}': {reason}", innerException)
    {
        Expression = expression;
    }
}
