namespace StateMaker;

public static class RuleBuilder
{
    private static readonly ExpressionEvaluator DefaultEvaluator = new();

    public static DeclarativeRule DefineRule(
        string name,
        string condition,
        Dictionary<string, string> transformations)
    {
        return new DeclarativeRule(name, condition, transformations, DefaultEvaluator);
    }

    public static DeclarativeRule DefineRule(
        string name,
        string condition,
        Dictionary<string, string> transformations,
        IExpressionEvaluator evaluator)
    {
        return new DeclarativeRule(name, condition, transformations, evaluator);
    }
}
