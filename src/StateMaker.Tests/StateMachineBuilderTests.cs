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

    [Fact]
    public void Build_MaxDepth_StopsExplorationBeyondConfiguredDepth()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["counter"] = 0;
        var rules = new IRule[] { new TestRule(
            _ => true,
            s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxDepth = 2 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(3, result.States.Count);
        Assert.Contains(result.States.Values, s => (int)s.Variables["counter"]! == 0);
        Assert.Contains(result.States.Values, s => (int)s.Variables["counter"]! == 1);
        Assert.Contains(result.States.Values, s => (int)s.Variables["counter"]! == 2);
    }

    [Fact]
    public void Build_MaxStates_StopsAddingStatesAtLimit()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["counter"] = 0;
        var rules = new IRule[] { new TestRule(
            _ => true,
            s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 3 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(3, result.States.Count);
    }

    [Fact]
    public void Build_DFS_ExploresDepthFirstOrder()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                _ => true,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; }),
            new TestRule(
                _ => true,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 10; return c; })
        };
        var config = new BuilderConfig
        {
            ExplorationStrategy = ExplorationStrategy.DEPTHFIRSTSEARCH,
            MaxStates = 5
        };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(5, result.States.Count);
        var stateValues = result.States.Values.Select(s => (int)s.Variables["counter"]!).OrderBy(v => v).ToArray();
        int[] expectedDfs = { 0, 1, 10, 11, 20 };
        Assert.Equal(expectedDfs, stateValues);
    }

    [Fact]
    public void Build_BFS_ExploresBreadthFirstOrder()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                _ => true,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; }),
            new TestRule(
                _ => true,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 10; return c; })
        };
        var config = new BuilderConfig
        {
            ExplorationStrategy = ExplorationStrategy.BREADTHFIRSTSEARCH,
            MaxStates = 5
        };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(5, result.States.Count);
        var stateValues = result.States.Values.Select(s => (int)s.Variables["counter"]!).OrderBy(v => v).ToArray();
        int[] expectedBfs = { 0, 1, 2, 10, 11 };
        Assert.Equal(expectedBfs, stateValues);
    }

    [Fact]
    public void Build_nullInitialState_ThrowsArgumentNullException()
    {
        var builder = new StateMachineBuilder();
        var rules = new IRule[] { new TestRule(_ => true, s => s.Clone()) };
        var config = new BuilderConfig();

        var ex = Assert.Throws<ArgumentNullException>(() => builder.Build(null!, rules, config));
        Assert.Equal("initialState", ex.ParamName);
    }

    [Fact]
    public void Build_nullRulesArray_ThrowsArgumentNullException()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        var config = new BuilderConfig();

        var ex = Assert.Throws<ArgumentNullException>(() => builder.Build(initialState, null!, config));
        Assert.Equal("rules", ex.ParamName);
    }

    [Fact]
    public void Build_nullBuildConfig_ThrowsArgumentNullException()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        var rules = new IRule[] { new TestRule(_ => true, s => s.Clone()) };

        var ex = Assert.Throws<ArgumentNullException>(() => builder.Build(initialState, rules, null!));
        Assert.Equal("config", ex.ParamName);
    }

    [Fact]
    public void Build_RulesArrayWithNullRuleInIt()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                _ => true,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; }),
            null!
        };
        var config = new BuilderConfig
        {
            ExplorationStrategy = ExplorationStrategy.BREADTHFIRSTSEARCH,
            MaxStates = 5
        };

        var ex = Assert.Throws<ArgumentNullException>(() => builder.Build(initialState, rules, config));
        Assert.Equal("rules[1]", ex.ParamName);
    }
}