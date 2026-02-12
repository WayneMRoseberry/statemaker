namespace StateMaker.Tests;

public class StateMachineBuilderTests
{
    private sealed class TestRule : IRule
    {
        private readonly Func<State, bool> _isAvailable;
        private readonly Func<State, State> _execute;

        public TestRule(Func<State, bool> isAvailable, Func<State, State> execute)
        {
            _isAvailable = isAvailable;
            _execute = execute;
        }

        public bool IsAvailable(State state) => _isAvailable(state);
        public State Execute(State state) => _execute(state);
    }

    [Fact]
    public void Build_ReturnsStateMachine()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        var rules = new IRule[] { new TestRule(_ => false, s => s.Clone()) };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.NotNull(result);
    }

    [Fact]
    public void Build_InitialState_IsInStateMachine()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["status"] = "start";
        var rules = new IRule[] { new TestRule(_ => false, s => s.Clone()) };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Single(result.States);
        Assert.Equal(initialState, result.States.Values.First());
    }

    [Fact]
    public void Build_SetsStartingStateId()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        var rules = new IRule[] { new TestRule(_ => false, s => s.Clone()) };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.NotNull(result.StartingStateId);
        Assert.True(result.States.ContainsKey(result.StartingStateId!));
    }

    [Fact]
    public void Build_OnlyOneStateBecauseRuleReturnsCloneOfInitialState()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        var rules = new IRule[] { new TestRule(_ => false, s => s.Clone()) };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.NotNull(result.StartingStateId);
        Assert.Single(result.States);
    }

    [Fact]
    public void Build_ShouldBeZeroTransitionsBecauseRuleReturnedFalseOnAvailable()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        var rules = new IRule[] { new TestRule(_ => false, s => s.Clone()) };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.NotNull(result.StartingStateId);
        Assert.Empty(result.Transitions);
    }

    [Fact]
    public void Build_ShouldBeOneTransitionsBecauseRuleReturnedTrueOnAvailable()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        var rules = new IRule[] { new TestRule(_ => true, s => s.Clone()) };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.NotNull(result.StartingStateId);
        Assert.Single(result.Transitions);
    }

    [Fact]
    public void Build_ShouldBeTwoStatesBecauseRuleMadeNewState()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["status"] = "Initial";
        var rules = new IRule[] { new TestRule(
            _ => true,
            s => { var c = s.Clone(); c.Variables["status"] = "NewValue"; return c; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(2, result.States.Count);

        var firstState = result.States[result.StartingStateId!];
        Assert.Equal("Initial", firstState.Variables["status"]);

        var secondStateEntry = result.States.First(kvp => kvp.Key != result.StartingStateId);
        Assert.Single(secondStateEntry.Value.Variables);
        Assert.Equal("NewValue", secondStateEntry.Value.Variables["status"]);

        Assert.Contains(result.Transitions, t =>
            t.SourceStateId == result.StartingStateId &&
            t.TargetStateId == secondStateEntry.Key);
    }
}