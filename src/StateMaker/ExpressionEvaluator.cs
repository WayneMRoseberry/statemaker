using NCalc;

namespace StateMaker;

public class ExpressionEvaluator : IExpressionEvaluator
{
    public bool EvaluateBoolean(string expression, Dictionary<string, object> variables)
    {
        var result = Evaluate(expression, variables);
        if (result is bool boolResult)
            return boolResult;
        throw new ExpressionEvaluationException(expression,
            $"Expected a boolean result but got: {result?.GetType().Name ?? "null"}");
    }

    public bool EvaluateBooleanLenient(string expression, Dictionary<string, object> variables)
    {
        var result = Evaluate(expression, variables, undefinedAsNull: true);
        if (result is bool boolResult)
            return boolResult;
        throw new ExpressionEvaluationException(expression,
            $"Expected a boolean result but got: {result?.GetType().Name ?? "null"}");
    }

    public object? EvaluateLenient(string expression, Dictionary<string, object> variables)
    {
        return Evaluate(expression, variables, undefinedAsNull: true);
    }

    public object Evaluate(string expression, Dictionary<string, object> variables)
    {
        return Evaluate(expression, variables, undefinedAsNull: false);
    }

    private static object Evaluate(string expression, Dictionary<string, object> variables, bool undefinedAsNull)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(variables);

        Expression ncalcExpr;
        try
        {
            ncalcExpr = new Expression(expression, ExpressionOptions.NoCache);
        }
        catch (Exception ex)
        {
            throw new ExpressionEvaluationException(expression,
                "Invalid syntax.", ex);
        }

        // Check for syntax errors before evaluating
        if (ncalcExpr.HasErrors())
        {
            throw new ExpressionEvaluationException(expression,
                "Invalid syntax.");
        }

        // Set parameters from variables — case-sensitive exact match
        foreach (var kvp in variables)
        {
            ncalcExpr.Parameters[kvp.Key] = kvp.Value;
        }

        // When undefinedAsNull is true, supply null for any parameter not in the variables dict
        if (undefinedAsNull)
        {
            ncalcExpr.EvaluateParameter += (name, args) =>
            {
                if (!variables.ContainsKey(name))
                    args.Result = null;
            };
        }

        try
        {
            var result = ncalcExpr.Evaluate();

            // NCalc returns Infinity for division by zero instead of throwing
            if (result is double d && (double.IsInfinity(d) || double.IsNaN(d)))
            {
                throw new ExpressionEvaluationException(expression,
                    "Division by zero.");
            }

            return result!;
        }
        catch (ExpressionEvaluationException)
        {
            throw;
        }
        catch (DivideByZeroException ex)
        {
            throw new ExpressionEvaluationException(expression,
                "Division by zero.", ex);
        }
        catch (Exception ex) when (ex.Message.Contains("not defined", StringComparison.OrdinalIgnoreCase)
                                   || ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            // NCalc throws when a parameter referenced in the expression is not in Parameters dict.
            // This commonly happens when a user writes a string literal without single quotes,
            // e.g. "activity": "jumping" instead of "activity": "'jumping'"
            throw new ExpressionEvaluationException(expression,
                "Undefined parameter. If you intended a string literal, wrap it in single quotes (e.g. 'value' instead of value).", ex);
        }
        catch (Exception ex)
        {
            throw new ExpressionEvaluationException(expression,
                "Evaluation failed.", ex);
        }
    }
}
