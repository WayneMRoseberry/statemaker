namespace StateMaker.Tests;

public class DeclarativeRuleTests
{
    private readonly ExpressionEvaluator _evaluator = new();

    private static Dictionary<string, string> Transforms(params (string key, string expr)[] pairs)
    {
        var dict = new Dictionary<string, string>();
        foreach (var (key, expr) in pairs)
            dict[key] = expr;
        return dict;
    }

    private static State MakeState(params (string key, object value)[] pairs)
    {
        var state = new State();
        foreach (var (key, value) in pairs)
            state.Variables[key] = value;
        return state;
    }

    #region 6.1 — Construction and GetName

    [Fact]
    public void GetName_ReturnsConfiguredName()
    {
        var rule = new DeclarativeRule("MyRule", "true", Transforms(), _evaluator);
        Assert.Equal("MyRule", rule.GetName());
    }

    [Fact]
    public void Constructor_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeclarativeRule(null!, "true", Transforms(), _evaluator));
    }

    [Fact]
    public void Constructor_NullCondition_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeclarativeRule("Rule", null!, Transforms(), _evaluator));
    }

    [Fact]
    public void Constructor_NullTransformations_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeclarativeRule("Rule", "true", null!, _evaluator));
    }

    [Fact]
    public void Constructor_NullEvaluator_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeclarativeRule("Rule", "true", Transforms(), null!));
    }

    #endregion

    #region 6.2 — IsAvailable

    [Fact]
    public void IsAvailable_ConditionTrue_ReturnsTrue()
    {
        var rule = new DeclarativeRule("Rule", "[Status] == 'Pending'",
            Transforms(), _evaluator);
        var state = MakeState(("Status", "Pending"));
        Assert.True(rule.IsAvailable(state));
    }

    [Fact]
    public void IsAvailable_ConditionFalse_ReturnsFalse()
    {
        var rule = new DeclarativeRule("Rule", "[Status] == 'Pending'",
            Transforms(), _evaluator);
        var state = MakeState(("Status", "Approved"));
        Assert.False(rule.IsAvailable(state));
    }

    [Fact]
    public void IsAvailable_CompoundCondition_ReturnsTrue()
    {
        var rule = new DeclarativeRule("Rule",
            "[Status] == 'Pending' && [Amount] < 1000",
            Transforms(), _evaluator);
        var state = MakeState(("Status", "Pending"), ("Amount", 500));
        Assert.True(rule.IsAvailable(state));
    }

    [Fact]
    public void IsAvailable_CompoundCondition_ReturnsFalse()
    {
        var rule = new DeclarativeRule("Rule",
            "[Status] == 'Pending' && [Amount] < 1000",
            Transforms(), _evaluator);
        var state = MakeState(("Status", "Pending"), ("Amount", 1500));
        Assert.False(rule.IsAvailable(state));
    }

    [Fact]
    public void IsAvailable_AlwaysTrue_ReturnsTrue()
    {
        var rule = new DeclarativeRule("Rule", "true", Transforms(), _evaluator);
        var state = MakeState(("x", 1));
        Assert.True(rule.IsAvailable(state));
    }

    [Fact]
    public void IsAvailable_BooleanVariable_Works()
    {
        var rule = new DeclarativeRule("Rule", "[IsActive] == true",
            Transforms(), _evaluator);
        var state = MakeState(("IsActive", true));
        Assert.True(rule.IsAvailable(state));
    }

    #endregion

    #region 6.3 — Execute

    [Fact]
    public void Execute_SingleTransformation_SetsValue()
    {
        var rule = new DeclarativeRule("Approve", "[Status] == 'Pending'",
            Transforms(("Status", "'Approved'")), _evaluator);
        var state = MakeState(("Status", "Pending"));

        var result = rule.Execute(state);

        Assert.Equal("Approved", result.Variables["Status"]);
    }

    [Fact]
    public void Execute_MultipleTransformations_SetsAllValues()
    {
        var rule = new DeclarativeRule("Process", "true",
            Transforms(("Status", "'Processed'"), ("Count", "[Count] + 1")),
            _evaluator);
        var state = MakeState(("Status", "Pending"), ("Count", 5));

        var result = rule.Execute(state);

        Assert.Equal("Processed", result.Variables["Status"]);
        Assert.Equal(6, result.Variables["Count"]);
    }

    [Fact]
    public void Execute_TransformationUsesOriginalState_NotIntermediateValues()
    {
        // Both transformations should read from the original state
        // If x=1, y=2: newX = y (should be 2), newY = x (should be 1)
        var rule = new DeclarativeRule("Swap", "true",
            Transforms(("x", "[y]"), ("y", "[x]")),
            _evaluator);
        var state = MakeState(("x", 1), ("y", 2));

        var result = rule.Execute(state);

        Assert.Equal(2, result.Variables["x"]);
        Assert.Equal(1, result.Variables["y"]);
    }

    [Fact]
    public void Execute_InputStateNotMutated()
    {
        var rule = new DeclarativeRule("Increment", "true",
            Transforms(("Count", "[Count] + 1")), _evaluator);
        var state = MakeState(("Count", 5));

        rule.Execute(state);

        Assert.Equal(5, state.Variables["Count"]);
    }

    [Fact]
    public void Execute_ArithmeticTransformation()
    {
        var rule = new DeclarativeRule("Double", "true",
            Transforms(("Value", "[Value] * 2")), _evaluator);
        var state = MakeState(("Value", 10));

        var result = rule.Execute(state);

        Assert.Equal(20, result.Variables["Value"]);
    }

    [Fact]
    public void Execute_NoTransformations_ReturnsClone()
    {
        var rule = new DeclarativeRule("NoOp", "true", Transforms(), _evaluator);
        var state = MakeState(("x", 1));

        var result = rule.Execute(state);

        Assert.Equal(state, result);
        Assert.NotSame(state, result);
    }

    [Fact]
    public void Execute_AddsNewVariable()
    {
        var rule = new DeclarativeRule("AddVar", "true",
            Transforms(("NewVar", "'hello'")), _evaluator);
        var state = MakeState(("x", 1));

        var result = rule.Execute(state);

        Assert.Equal("hello", result.Variables["NewVar"]);
        Assert.Equal(1, result.Variables["x"]);
    }

    [Fact]
    public void Execute_BooleanTransformation()
    {
        var rule = new DeclarativeRule("Deactivate", "true",
            Transforms(("IsActive", "false")), _evaluator);
        var state = MakeState(("IsActive", true));

        var result = rule.Execute(state);

        Assert.Equal(false, result.Variables["IsActive"]);
    }

    #endregion

    #region 6.3 — Execute Error Cases

    [Fact]
    public void Execute_InvalidTransformationExpression_Throws()
    {
        var rule = new DeclarativeRule("Bad", "true",
            Transforms(("x", "=== invalid ===")), _evaluator);
        var state = MakeState(("x", 1));

        Assert.Throws<ExpressionEvaluationException>(() => rule.Execute(state));
    }

    [Fact]
    public void IsAvailable_InvalidCondition_Throws()
    {
        var rule = new DeclarativeRule("Bad", "=== invalid ===",
            Transforms(), _evaluator);
        var state = MakeState(("x", 1));

        Assert.Throws<ExpressionEvaluationException>(() => rule.IsAvailable(state));
    }

    #endregion

    #region IRule Contract

    [Fact]
    public void ImplementsIRule()
    {
        var rule = new DeclarativeRule("Rule", "true", Transforms(), _evaluator);
        Assert.IsAssignableFrom<IRule>(rule);
    }

    [Fact]
    public void WorksWithStateMachineBuilder()
    {
        // Integration: DeclarativeRule works with the builder to produce a state machine
        var rule = new DeclarativeRule("Increment", "[step] < 3",
            Transforms(("step", "[step] + 1")), _evaluator);
        var initialState = MakeState(("step", 0));
        var config = new BuilderConfig { MaxStates = 10 };

        var builder = new StateMachineBuilder();
        var machine = builder.Build(initialState, new IRule[] { rule }, config);

        // Should produce states: step=0, step=1, step=2, step=3
        Assert.Equal(4, machine.States.Count);
        Assert.Equal(3, machine.Transitions.Count);
        Assert.True(machine.IsValidMachine());
    }

    #endregion
}
