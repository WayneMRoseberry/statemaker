namespace StateMaker.Tests;

public class ExpressionEvaluatorTests
{
    private readonly ExpressionEvaluator _evaluator = new();

    private static Dictionary<string, object> Vars(params (string key, object value)[] pairs)
    {
        var dict = new Dictionary<string, object>();
        foreach (var (key, value) in pairs)
            dict[key] = value;
        return dict;
    }

    #region 5.3 — Comparison Operators

    [Fact]
    public void EvaluateBoolean_Equality_StringEquals_ReturnsTrue()
    {
        var result = _evaluator.EvaluateBoolean("[Status] == 'Pending'", Vars(("Status", "Pending")));
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBoolean_Equality_StringNotEqual_ReturnsFalse()
    {
        var result = _evaluator.EvaluateBoolean("[Status] == 'Pending'", Vars(("Status", "Approved")));
        Assert.False(result);
    }

    [Fact]
    public void EvaluateBoolean_Inequality_ReturnsTrueWhenDifferent()
    {
        var result = _evaluator.EvaluateBoolean("[Status] != 'Closed'", Vars(("Status", "Open")));
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBoolean_IntEquals_ReturnsTrue()
    {
        var result = _evaluator.EvaluateBoolean("[Count] == 5", Vars(("Count", 5)));
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBoolean_LessThan_ReturnsTrue()
    {
        var result = _evaluator.EvaluateBoolean("[Amount] < 1000", Vars(("Amount", 500)));
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBoolean_LessThan_ReturnsFalse()
    {
        var result = _evaluator.EvaluateBoolean("[Amount] < 1000", Vars(("Amount", 1500)));
        Assert.False(result);
    }

    [Fact]
    public void EvaluateBoolean_GreaterThan_ReturnsTrue()
    {
        var result = _evaluator.EvaluateBoolean("[Amount] > 100", Vars(("Amount", 500)));
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBoolean_LessThanOrEqual_BoundaryTrue()
    {
        var result = _evaluator.EvaluateBoolean("[x] <= 10", Vars(("x", 10)));
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBoolean_GreaterThanOrEqual_BoundaryTrue()
    {
        var result = _evaluator.EvaluateBoolean("[x] >= 10", Vars(("x", 10)));
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBoolean_GreaterThanOrEqual_BoundaryFalse()
    {
        var result = _evaluator.EvaluateBoolean("[x] >= 10", Vars(("x", 9)));
        Assert.False(result);
    }

    #endregion

    #region 5.4 — Logical Operators

    [Fact]
    public void EvaluateBoolean_LogicalAnd_BothTrue()
    {
        var result = _evaluator.EvaluateBoolean("[Status] == 'Pending' && [Amount] < 1000",
            Vars(("Status", "Pending"), ("Amount", 500)));
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBoolean_LogicalAnd_OneFalse()
    {
        var result = _evaluator.EvaluateBoolean("[Status] == 'Pending' && [Amount] < 1000",
            Vars(("Status", "Pending"), ("Amount", 1500)));
        Assert.False(result);
    }

    [Fact]
    public void EvaluateBoolean_LogicalOr_OneFalseOneTrue()
    {
        var result = _evaluator.EvaluateBoolean("[x] == 1 || [x] == 2",
            Vars(("x", 2)));
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBoolean_LogicalOr_BothFalse()
    {
        var result = _evaluator.EvaluateBoolean("[x] == 1 || [x] == 2",
            Vars(("x", 3)));
        Assert.False(result);
    }

    [Fact]
    public void EvaluateBoolean_LogicalNot_NegatesTrue()
    {
        var result = _evaluator.EvaluateBoolean("!([Status] == 'Closed')",
            Vars(("Status", "Open")));
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBoolean_LogicalNot_NegatesFalse()
    {
        var result = _evaluator.EvaluateBoolean("!([Status] == 'Open')",
            Vars(("Status", "Open")));
        Assert.False(result);
    }

    [Fact]
    public void EvaluateBoolean_CompoundLogical_AndOrNot()
    {
        // (Active == true AND Status != 'Closed') OR Override == true
        var result = _evaluator.EvaluateBoolean(
            "([Active] == true && [Status] != 'Closed') || [Override] == true",
            Vars(("Active", false), ("Status", "Open"), ("Override", true)));
        Assert.True(result);
    }

    #endregion

    #region 5.5 — Literal Values

    [Fact]
    public void Evaluate_StringLiteral_SingleQuotes()
    {
        var result = _evaluator.Evaluate("'Hello'", Vars());
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Evaluate_IntegerLiteral()
    {
        var result = _evaluator.Evaluate("42", Vars());
        Assert.Equal(42, result);
    }

    [Fact]
    public void Evaluate_NegativeIntegerLiteral()
    {
        var result = _evaluator.Evaluate("-7", Vars());
        Assert.Equal(-7, result);
    }

    [Fact]
    public void Evaluate_BooleanLiteralTrue()
    {
        var result = _evaluator.Evaluate("true", Vars());
        Assert.Equal(true, result);
    }

    [Fact]
    public void Evaluate_BooleanLiteralFalse()
    {
        var result = _evaluator.Evaluate("false", Vars());
        Assert.Equal(false, result);
    }

    [Fact]
    public void Evaluate_FloatLiteral()
    {
        var result = _evaluator.Evaluate("3.14", Vars());
        Assert.Equal(3.14, result);
    }

    [Fact]
    public void Evaluate_ZeroLiteral()
    {
        var result = _evaluator.Evaluate("0", Vars());
        Assert.Equal(0, result);
    }

    #endregion

    #region 5.6 — Variable Resolution

    [Fact]
    public void Evaluate_VariableResolution_String()
    {
        var result = _evaluator.Evaluate("[Status]", Vars(("Status", "Active")));
        Assert.Equal("Active", result);
    }

    [Fact]
    public void Evaluate_VariableResolution_Int()
    {
        var result = _evaluator.Evaluate("[Count]", Vars(("Count", 42)));
        Assert.Equal(42, result);
    }

    [Fact]
    public void Evaluate_VariableResolution_Bool()
    {
        var result = _evaluator.Evaluate("[IsActive]", Vars(("IsActive", true)));
        Assert.Equal(true, result);
    }

    [Fact]
    public void Evaluate_VariableResolution_Double()
    {
        var result = _evaluator.Evaluate("[Price]", Vars(("Price", 9.99)));
        Assert.Equal(9.99, result);
    }

    [Fact]
    public void Evaluate_VariableResolution_CaseSensitive()
    {
        // "status" and "Status" are different variables
        var result = _evaluator.Evaluate("[Status]",
            Vars(("Status", "Found"), ("status", "NotThis")));
        Assert.Equal("Found", result);
    }

    [Fact]
    public void Evaluate_MultipleVariables()
    {
        var result = _evaluator.EvaluateBoolean("[x] < [y]",
            Vars(("x", 3), ("y", 10)));
        Assert.True(result);
    }

    #endregion

    #region 5.7 — Arithmetic Operators

    [Fact]
    public void Evaluate_Addition_Integers()
    {
        var result = _evaluator.Evaluate("[Count] + 1", Vars(("Count", 5)));
        Assert.Equal(6, result);
    }

    [Fact]
    public void Evaluate_Subtraction()
    {
        var result = _evaluator.Evaluate("[Total] - [Discount]",
            Vars(("Total", 100), ("Discount", 15)));
        Assert.Equal(85, result);
    }

    [Fact]
    public void Evaluate_Multiplication()
    {
        var result = _evaluator.Evaluate("[Price] * [Quantity]",
            Vars(("Price", 10), ("Quantity", 3)));
        Assert.Equal(30, result);
    }

    [Fact]
    public void Evaluate_Division()
    {
        var result = _evaluator.Evaluate("[Total] / [Parts]",
            Vars(("Total", 100.0), ("Parts", 4.0)));
        Assert.Equal(25.0, result);
    }

    [Fact]
    public void Evaluate_FloatArithmetic()
    {
        var result = _evaluator.Evaluate("[Amount] * 0.9",
            Vars(("Amount", 100.0)));
        Assert.Equal(90.0, result);
    }

    [Fact]
    public void Evaluate_Parentheses()
    {
        var result = _evaluator.Evaluate("([Price] * [Quantity]) - [Discount]",
            Vars(("Price", 10), ("Quantity", 3), ("Discount", 5)));
        Assert.Equal(25, result);
    }

    [Fact]
    public void Evaluate_NestedParentheses()
    {
        var result = _evaluator.Evaluate("(([a] + [b]) * ([c] - [d]))",
            Vars(("a", 2), ("b", 3), ("c", 10), ("d", 4)));
        // (2 + 3) * (10 - 4) = 5 * 6 = 30
        Assert.Equal(30, result);
    }

    [Fact]
    public void Evaluate_OperatorPrecedence_MultiplicationBeforeAddition()
    {
        var result = _evaluator.Evaluate("[a] + [b] * [c]",
            Vars(("a", 2), ("b", 3), ("c", 4)));
        // 2 + (3 * 4) = 14
        Assert.Equal(14, result);
    }

    #endregion

    #region 5.8 — Error Handling

    [Fact]
    public void Evaluate_UndefinedVariable_Throws()
    {
        var ex = Assert.Throws<ExpressionEvaluationException>(() =>
            _evaluator.Evaluate("[MissingVar] + 1", Vars(("Other", 5))));
        Assert.Contains("MissingVar", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EvaluateBoolean_UndefinedVariable_Throws()
    {
        var ex = Assert.Throws<ExpressionEvaluationException>(() =>
            _evaluator.EvaluateBoolean("[Missing] == true", Vars()));
        Assert.Contains("Missing", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluate_InvalidSyntax_Throws()
    {
        var ex = Assert.Throws<ExpressionEvaluationException>(() =>
            _evaluator.Evaluate("=== invalid ===", Vars()));
        Assert.Contains("syntax", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_DivisionByZero_Throws()
    {
        var ex = Assert.Throws<ExpressionEvaluationException>(() =>
            _evaluator.Evaluate("[x] / 0", Vars(("x", 10))));
        Assert.Contains("Division by zero", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EvaluateBoolean_NonBooleanExpression_Throws()
    {
        var ex = Assert.Throws<ExpressionEvaluationException>(() =>
            _evaluator.EvaluateBoolean("[x] + 1", Vars(("x", 5))));
        Assert.Contains("Expected a boolean result", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluate_NullExpression_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _evaluator.Evaluate(null!, Vars()));
    }

    [Fact]
    public void Evaluate_NullVariables_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _evaluator.Evaluate("1 + 1", null!));
    }

    [Fact]
    public void Evaluate_EmptyExpression_Throws()
    {
        Assert.Throws<ExpressionEvaluationException>(() =>
            _evaluator.Evaluate("", Vars()));
    }

    #endregion

    #region 5.9 — Sandboxing

    [Fact]
    public void Evaluate_NoArbitraryCodeExecution_PureExpressionOnly()
    {
        // NCalc only evaluates mathematical/logical expressions
        // It cannot call .NET methods, access files, or make network calls.
        // This test verifies that function-like syntax fails (no custom functions registered).
        Assert.Throws<ExpressionEvaluationException>(() =>
            _evaluator.Evaluate("System.IO.File.ReadAllText('test.txt')", Vars()));
    }

    [Fact]
    public void Evaluate_NoReflection()
    {
        Assert.Throws<ExpressionEvaluationException>(() =>
            _evaluator.Evaluate("GetType().Assembly", Vars()));
    }

    #endregion

    #region Compound Expressions

    [Fact]
    public void EvaluateBoolean_CompoundComparison_IntAndString()
    {
        var result = _evaluator.EvaluateBoolean(
            "[Status] == 'Pending' && [Amount] < 1000 && [IsActive] == true",
            Vars(("Status", "Pending"), ("Amount", 500), ("IsActive", true)));
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_ArithmeticWithComparison()
    {
        var result = _evaluator.EvaluateBoolean(
            "([Price] * [Quantity]) > 100",
            Vars(("Price", 25), ("Quantity", 5)));
        Assert.True(result); // 125 > 100
    }

    [Fact]
    public void Evaluate_TransformationExpression_IncrementVariable()
    {
        var result = _evaluator.Evaluate("[Count] + 1", Vars(("Count", 5)));
        Assert.Equal(6, result);
    }

    [Fact]
    public void Evaluate_TransformationExpression_StringLiteral()
    {
        var result = _evaluator.Evaluate("'Approved'", Vars());
        Assert.Equal("Approved", result);
    }

    [Fact]
    public void EvaluateBoolean_EmptyVariables_LiteralExpression()
    {
        var result = _evaluator.EvaluateBoolean("true", Vars());
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBoolean_BoolVariable_DirectComparison()
    {
        var result = _evaluator.EvaluateBoolean("[IsApproved] == true",
            Vars(("IsApproved", false)));
        Assert.False(result);
    }

    #endregion

    #region EvaluateBooleanLenient — Undefined Parameters as Null

    [Fact]
    public void EvaluateBooleanLenient_UndefinedEqualsString_ReturnsFalse()
    {
        // null == 'Action1' → false
        var result = _evaluator.EvaluateBooleanLenient(
            "name == 'Action1'", Vars());
        Assert.False(result);
    }

    [Fact]
    public void EvaluateBooleanLenient_UndefinedNotEqualString_ReturnsTrue()
    {
        // null != 'done' → true
        var result = _evaluator.EvaluateBooleanLenient(
            "buy != 'done'", Vars());
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBooleanLenient_CompoundWithUndefined_ReturnsCorrectly()
    {
        // Defined variables evaluate normally, undefined treated as null
        var result = _evaluator.EvaluateBooleanLenient(
            "displayed != 'options' && step > 1 && cart == 'empty' && buy != 'done'",
            Vars(("displayed", "fish"), ("step", 2), ("cart", "empty")));
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBooleanLenient_AllDefined_WorksNormally()
    {
        var result = _evaluator.EvaluateBooleanLenient(
            "[Status] == 'Pending' && [Count] < 10",
            Vars(("Status", "Pending"), ("Count", 5)));
        Assert.True(result);
    }

    [Fact]
    public void EvaluateBoolean_Strict_UndefinedVariable_StillThrows()
    {
        // The strict EvaluateBoolean should still throw for undefined params
        Assert.Throws<ExpressionEvaluationException>(() =>
            _evaluator.EvaluateBoolean("buy != 'done'", Vars()));
    }

    #endregion
}
