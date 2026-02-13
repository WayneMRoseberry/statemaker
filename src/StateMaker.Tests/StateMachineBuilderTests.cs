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
        Assert.True(result.IsValidMachine());
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
        Assert.True(result.IsValidMachine());
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
        Assert.True(result.IsValidMachine());
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
        Assert.True(result.IsValidMachine());
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
        Assert.True(result.IsValidMachine());
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
        Assert.True(result.IsValidMachine());
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
        Assert.True(result.IsValidMachine());
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
        Assert.True(result.IsValidMachine());
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
        Assert.True(result.IsValidMachine());
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
        Assert.True(result.IsValidMachine());
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
        Assert.True(result.IsValidMachine());
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

    [Fact]
    public void Build_LinearStateChain_TransitionsFormChain()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[] { new TestRule(
            _ => true,
            s => { var c = s.Clone(); c.Variables["step"] = (int)c.Variables["step"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 4 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(4, result.States.Count);
        Assert.Equal(3, result.Transitions.Count);

        var idByStep = result.States
            .ToDictionary(kvp => (int)kvp.Value.Variables["step"]!, kvp => kvp.Key);

        Assert.Contains(result.Transitions, t => t.SourceStateId == idByStep[0] && t.TargetStateId == idByStep[1]);
        Assert.Contains(result.Transitions, t => t.SourceStateId == idByStep[1] && t.TargetStateId == idByStep[2]);
        Assert.Contains(result.Transitions, t => t.SourceStateId == idByStep[2] && t.TargetStateId == idByStep[3]);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_BranchingStates_TwoBranchesFromInitial()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["branch"] = "start";
        var rules = new IRule[]
        {
            new TestRule(
                s => (string)s.Variables["branch"]! == "start",
                s => { var c = s.Clone(); c.Variables["branch"] = "A"; return c; }),
            new TestRule(
                s => (string)s.Variables["branch"]! == "start",
                s => { var c = s.Clone(); c.Variables["branch"] = "B"; return c; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(3, result.States.Count);
        Assert.Equal(2, result.Transitions.Count);

        var idByBranch = result.States
            .ToDictionary(kvp => (string)kvp.Value.Variables["branch"]!, kvp => kvp.Key);

        Assert.Contains(result.Transitions, t => t.SourceStateId == idByBranch["start"] && t.TargetStateId == idByBranch["A"]);
        Assert.Contains(result.Transitions, t => t.SourceStateId == idByBranch["start"] && t.TargetStateId == idByBranch["B"]);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_CycleDetection_RecordsTransitionToExistingState()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["toggle"] = true;
        var rules = new IRule[] { new TestRule(
            _ => true,
            s => { var c = s.Clone(); c.Variables["toggle"] = !(bool)c.Variables["toggle"]!; return c; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(2, result.States.Count);

        var idByToggle = result.States
            .ToDictionary(kvp => (bool)kvp.Value.Variables["toggle"]!, kvp => kvp.Key);

        Assert.Equal(2, result.Transitions.Count);
        Assert.Contains(result.Transitions, t => t.SourceStateId == idByToggle[true] && t.TargetStateId == idByToggle[false]);
        Assert.Contains(result.Transitions, t => t.SourceStateId == idByToggle[false] && t.TargetStateId == idByToggle[true]);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_EmptyRulesArray_ReturnsSingleStateNoTransitions()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["status"] = "alone";
        var rules = Array.Empty<IRule>();
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.Equal("alone", result.States[result.StartingStateId!].Variables["status"]);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_AllRulesAlwaysAvailable_AllAppliedToEachState()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;
        initialState.Variables["y"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                _ => true,
                s => { var c = s.Clone(); c.Variables["x"] = (int)c.Variables["x"]! + 1; return c; }),
            new TestRule(
                _ => true,
                s => { var c = s.Clone(); c.Variables["y"] = (int)c.Variables["y"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 5 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(5, result.States.Count);
        Assert.Contains(result.States.Values, s => (int)s.Variables["x"]! == 1 && (int)s.Variables["y"]! == 0);
        Assert.Contains(result.States.Values, s => (int)s.Variables["x"]! == 0 && (int)s.Variables["y"]! == 1);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_TwoRulesProduceSameState_NoDuplicateAndBothTransitionsRecorded()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["value"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                s => (int)s.Variables["value"]! == 0,
                s => { var c = s.Clone(); c.Variables["value"] = 1; return c; }),
            new TestRule(
                s => (int)s.Variables["value"]! == 0,
                s => { var c = s.Clone(); c.Variables["value"] = 1; return c; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(2, result.States.Count);

        var targetId = result.States.First(kvp => kvp.Key != result.StartingStateId).Key;
        Assert.Equal(2, result.Transitions.Count);
        Assert.All(result.Transitions, t =>
        {
            Assert.Equal(result.StartingStateId, t.SourceStateId);
            Assert.Equal(targetId, t.TargetStateId);
        });
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_RuleThatBuildsUntilValueEqualsCertainAmount()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["value"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                s => (int)s.Variables["value"]! < 4,
                s => { var c = s.Clone(); c.Variables["value"] = (int)c.Variables["value"]! + 1; return c; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(5, result.States.Count);

        var targetId = result.States.First(kvp => kvp.Key != result.StartingStateId).Key;
        Assert.Equal(4, result.Transitions.Count);
        Assert.True(result.IsValidMachine());
    }
}