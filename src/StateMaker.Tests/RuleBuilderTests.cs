namespace StateMaker.Tests;

public class RuleBuilderTests
{
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

    [Fact]
    public void DefineRule_CreatesDeclarativeRule()
    {
        var rule = RuleBuilder.DefineRule("MyRule", "true", Transforms(("x", "1")));

        Assert.IsType<DeclarativeRule>(rule);
        Assert.Equal("MyRule", rule.GetName());
    }

    [Fact]
    public void DefineRule_RuleIsAvailable_Works()
    {
        var rule = RuleBuilder.DefineRule("Check", "[x] > 0", Transforms());
        var state = MakeState(("x", 5));

        Assert.True(rule.IsAvailable(state));
    }

    [Fact]
    public void DefineRule_RuleExecute_Works()
    {
        var rule = RuleBuilder.DefineRule("Increment", "true",
            Transforms(("Count", "[Count] + 1")));
        var state = MakeState(("Count", 10));

        var result = rule.Execute(state);

        Assert.Equal(11, result.Variables["Count"]);
    }

    [Fact]
    public void DefineRule_WithCustomEvaluator_UsesIt()
    {
        var evaluator = new ExpressionEvaluator();
        var rule = RuleBuilder.DefineRule("Rule", "[x] == 1",
            Transforms(("x", "2")), evaluator);

        var state = MakeState(("x", 1));
        Assert.True(rule.IsAvailable(state));

        var result = rule.Execute(state);
        Assert.Equal(2, result.Variables["x"]);
    }

    [Fact]
    public void DefineRule_ImplementsIRule()
    {
        var rule = RuleBuilder.DefineRule("Rule", "true", Transforms());
        Assert.IsAssignableFrom<IRule>(rule);
    }

    [Fact]
    public void DefineRule_WorksWithBuilder()
    {
        var rule = RuleBuilder.DefineRule("Step", "[step] < 2",
            Transforms(("step", "[step] + 1")));
        var initialState = MakeState(("step", 0));
        var config = new BuilderConfig { MaxStates = 10 };

        var builder = new StateMachineBuilder();
        var machine = builder.Build(initialState, new IRule[] { rule }, config);

        Assert.Equal(3, machine.States.Count);
        Assert.Equal(2, machine.Transitions.Count);
        Assert.True(machine.IsValidMachine());
    }

    [Fact]
    public void DefineRule_MultipleRules_WorkTogether()
    {
        var approve = RuleBuilder.DefineRule("Approve", "[Status] == 'Pending'",
            Transforms(("Status", "'Approved'")));
        var close = RuleBuilder.DefineRule("Close", "[Status] == 'Approved'",
            Transforms(("Status", "'Closed'")));

        var initialState = MakeState(("Status", "Pending"));
        var config = new BuilderConfig { MaxStates = 10 };

        var builder = new StateMachineBuilder();
        var machine = builder.Build(initialState, new IRule[] { approve, close }, config);

        // Pending -> Approved -> Closed
        Assert.Equal(3, machine.States.Count);
        Assert.Equal(2, machine.Transitions.Count);
        Assert.True(machine.IsValidMachine());
    }
}
