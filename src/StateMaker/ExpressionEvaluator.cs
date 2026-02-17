using NCalc;

namespace StateMaker;

public class ExpressionEvaluator : IExpressionEvaluator
{
    public bool EvaluateBoolean(string expression, Dictionary<string, object> variables)
    {
        var result = Evaluate(expression, variables);
        if (result is bool boolResult)
            return boolResult;
        throw new InvalidOperationException(
            $"Expression '{expression}' did not evaluate to a boolean value. Got: {result?.GetType().Name ?? "null"}");
    }

    public object Evaluate(string expression, Dictionary<string, object> variables)
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
            throw new InvalidOperationException(
                $"Invalid expression syntax: {ex.Message}", ex);
        }

        // Check for syntax errors before evaluating
        if (ncalcExpr.HasErrors())
        {
            throw new InvalidOperationException(
                $"Invalid expression syntax: {ncalcExpr.Error}");
        }

        // Set parameters from variables — case-sensitive exact match
        foreach (var kvp in variables)
        {
            ncalcExpr.Parameters[kvp.Key] = kvp.Value;
        }

        try
        {
            var result = ncalcExpr.Evaluate();

            // NCalc returns Infinity for division by zero instead of throwing
            if (result is double d && (double.IsInfinity(d) || double.IsNaN(d)))
            {
                throw new InvalidOperationException(
                    $"Division by zero in expression '{expression}'");
            }

            return result!;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (DivideByZeroException)
        {
            throw new InvalidOperationException(
                $"Division by zero in expression '{expression}'");
        }
        catch (Exception ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                                   || ex.Message.Contains("parameter", StringComparison.OrdinalIgnoreCase))
        {
            // NCalc throws when a parameter referenced in the expression is not in Parameters dict
            // Extract the variable name from the message if possible
            throw new InvalidOperationException(
                $"Variable not found in state. Expression: '{expression}'. Detail: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error evaluating expression '{expression}': {ex.Message}", ex);
        }
    }
}
