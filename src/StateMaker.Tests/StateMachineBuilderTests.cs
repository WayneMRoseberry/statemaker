namespace StateMaker.Tests;

public class StateMachineBuilderTests
{
    private sealed class StubRule : IRule
    {
        public bool IsAvailable(State state) => false;
        public State Execute(State state) => state.Clone();
    }

    private sealed class AlwaysAvailableRule : IRule
    {
        public bool IsAvailable(State state) => true;
        public State Execute(State state) => state.Clone();
    }

    [Fact]
    public void Build_ReturnsStateMachine()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        var rules = new IRule[] { new StubRule() };
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
        var rules = new IRule[] { new StubRule() };
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
        var rules = new IRule[] { new StubRule() };
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
        var rules = new IRule[] { new StubRule() };
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
        var rules = new IRule[] { new StubRule() };
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
        var rules = new IRule[] { new AlwaysAvailableRule() };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.NotNull(result.StartingStateId);
        Assert.Single(result.Transitions);
    }

}