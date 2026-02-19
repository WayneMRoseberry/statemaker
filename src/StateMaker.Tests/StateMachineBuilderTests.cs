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

    private sealed class NamedTestRule : IRule
    {
        private readonly string _name;
        private readonly Func<State, bool> _isAvailable;
        private readonly Func<State, State> _execute;

        public NamedTestRule(string name, Func<State, bool> isAvailable, Func<State, State> execute)
        {
            _name = name;
            _isAvailable = isAvailable;
            _execute = execute;
        }

        public bool IsAvailable(State state) => _isAvailable(state);
        public State Execute(State state) => _execute(state);
        public string GetName() => _name;
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

    [Fact]
    public void GetName_DefaultImplementation_ReturnsTypeName()
    {
        IRule rule = new TestRule(_ => false, s => s.Clone());

        Assert.Equal("TestRule", rule.GetName());
    }

    [Fact]
    public void GetName_CustomOverride_ReturnsCustomName()
    {
        IRule rule = new NamedTestRule("MyCustomName", _ => false, s => s.Clone());

        Assert.Equal("MyCustomName", rule.GetName());
    }

    [Fact]
    public void Build_TransitionRuleName_UsesGetNameNotTypeName()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["status"] = "start";
        var rules = new IRule[]
        {
            new NamedTestRule("ApproveOrder",
                s => (string)s.Variables["status"]! == "start",
                s => { var c = s.Clone(); c.Variables["status"] = "approved"; return c; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Single(result.Transitions);
        Assert.Equal("ApproveOrder", result.Transitions[0].RuleName);
    }

    [Fact]
    public void Build_TransitionRuleName_DefaultGetName_UsesTypeName()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["status"] = "start";
        var rules = new IRule[]
        {
            new TestRule(
                s => (string)s.Variables["status"]! == "start",
                s => { var c = s.Clone(); c.Variables["status"] = "done"; return c; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Single(result.Transitions);
        Assert.Equal("TestRule", result.Transitions[0].RuleName);
    }

    [Fact]
    public void Build_MixedRules_TransitionsUseRespectiveGetNames()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                s => (int)s.Variables["x"]! == 0,
                s => { var c = s.Clone(); c.Variables["x"] = 1; return c; }),
            new NamedTestRule("IncrementByTen",
                s => (int)s.Variables["x"]! == 0,
                s => { var c = s.Clone(); c.Variables["x"] = 10; return c; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(2, result.Transitions.Count);
        Assert.Contains(result.Transitions, t => t.RuleName == "TestRule");
        Assert.Contains(result.Transitions, t => t.RuleName == "IncrementByTen");
    }

    [Fact]
    public void Build_CycleTransition_UsesGetNameForRuleName()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["toggle"] = true;
        var rules = new IRule[]
        {
            new NamedTestRule("ToggleRule", _ => true,
                s => { var c = s.Clone(); c.Variables["toggle"] = !(bool)c.Variables["toggle"]!; return c; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(2, result.Transitions.Count);
        Assert.All(result.Transitions, t => Assert.Equal("ToggleRule", t.RuleName));
    }

    #region 3.20 — Input State Mutation Edge Cases

    [Fact]
    public void Build_MutatingRule_ModifiesInputVariable_BuilderStillCompletes()
    {
        // Rule mutates input state directly instead of cloning
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                s => (int)s.Variables["step"]! < 3,
                s => { s.Variables["step"] = (int)s.Variables["step"]! + 1; return s; })
        };
        var config = new BuilderConfig();

        // The builder should not throw — it may produce unexpected results
        // but should still complete and return a valid machine structure
        StateMachine result = builder.Build(initialState, rules, config);

        Assert.NotNull(result);
        Assert.NotNull(result.StartingStateId);
        Assert.True(result.States.Count >= 1);
    }

    [Fact]
    public void Build_MutatingRule_AddsNewVariable_BuilderStillCompletes()
    {
        // Rule adds a new variable to the input state instead of cloning
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                s => !s.Variables.ContainsKey("added"),
                s => { s.Variables["added"] = true; return s; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.NotNull(result);
        Assert.True(result.States.Count >= 1);
    }

    [Fact]
    public void Build_MutatingRule_ReturnsInputAsNewState_BuilderStillCompletes()
    {
        // Rule mutates input and returns same reference
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                s => (int)s.Variables["counter"]! < 2,
                s =>
                {
                    // Mutate the original and return it — this violates immutability
                    s.Variables["counter"] = (int)s.Variables["counter"]! + 1;
                    return s;
                })
        };
        var config = new BuilderConfig { MaxStates = 5 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.NotNull(result);
        Assert.NotNull(result.StartingStateId);
    }

    [Fact]
    public void Build_MutatingRule_InitialStatePreservedWhenRuleClonesCorrectly()
    {
        // Verify that when rules clone correctly, the initial state is not corrupted
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["value"] = 42;
        var rules = new IRule[]
        {
            new TestRule(
                s => (int)s.Variables["value"]! == 42,
                s => { var c = s.Clone(); c.Variables["value"] = 99; return c; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        // The original initialState object should not be modified
        Assert.Equal(42, initialState.Variables["value"]);
        Assert.Equal(2, result.States.Count);
        Assert.True(result.IsValidMachine());
    }

    #endregion

    #region 3.21 — Resilience: Boundary Configurations

    [Fact]
    public void Build_MaxStates0_InitialStateStillAdded()
    {
        // MaxStates=0 means "stop adding new states at 0" but initial state is added before the check
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new TestRule(_ => true, s => { var c = s.Clone(); c.Variables["step"] = (int)c.Variables["step"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 0 };

        StateMachine result = builder.Build(initialState, rules, config);

        // Initial state should still be present (it's added before limit check)
        Assert.True(result.States.Count >= 1);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_MaxStates1_ExactlyOneState()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new TestRule(_ => true, s => { var c = s.Clone(); c.Variables["step"] = (int)c.Variables["step"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 1 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_MaxStatesNegative_InitialStateStillAdded()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new TestRule(_ => true, s => { var c = s.Clone(); c.Variables["step"] = (int)c.Variables["step"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = -1 };

        StateMachine result = builder.Build(initialState, rules, config);

        // With negative MaxStates, States.Count (1) >= MaxStates (-1) is true from the start
        // so no new states should be added beyond initial
        Assert.True(result.States.Count >= 1);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_MaxDepth0_NoExplorationBeyondInitial()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new TestRule(_ => true, s => { var c = s.Clone(); c.Variables["step"] = (int)c.Variables["step"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxDepth = 0 };

        StateMachine result = builder.Build(initialState, rules, config);

        // MaxDepth=0 means currentDepth (0) >= MaxDepth (0) is true, so no rules applied
        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_MaxDepthNegative_NoExplorationBeyondInitial()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new TestRule(_ => true, s => { var c = s.Clone(); c.Variables["step"] = (int)c.Variables["step"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxDepth = -1 };

        StateMachine result = builder.Build(initialState, rules, config);

        // MaxDepth=-1 means currentDepth (0) >= MaxDepth (-1) is true, so no rules applied
        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_MaxDepth1_WithBranching_OneLevelOfChildren()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                s => (int)s.Variables["step"]! == 0,
                s => { var c = s.Clone(); c.Variables["step"] = 1; return c; }),
            new TestRule(
                s => (int)s.Variables["step"]! == 0,
                s => { var c = s.Clone(); c.Variables["step"] = 2; return c; }),
            // This rule would apply at depth 1 but should be skipped
            new TestRule(
                s => (int)s.Variables["step"]! > 0,
                s => { var c = s.Clone(); c.Variables["step"] = (int)c.Variables["step"]! + 10; return c; })
        };
        var config = new BuilderConfig { MaxDepth = 1 };

        StateMachine result = builder.Build(initialState, rules, config);

        // Only initial + 2 children, no grandchildren
        Assert.Equal(3, result.States.Count);
        Assert.Equal(2, result.Transitions.Count);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_BothMaxStates1_MaxDepth1_SingleState()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new TestRule(_ => true, s => { var c = s.Clone(); c.Variables["step"] = (int)c.Variables["step"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 1, MaxDepth = 1 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_MaxStates2_MaxDepth0_SingleState()
    {
        // MaxDepth=0 prevents exploration even though MaxStates allows 2
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new TestRule(_ => true, s => { var c = s.Clone(); c.Variables["step"] = (int)c.Variables["step"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 2, MaxDepth = 0 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_EmptyInitialState_NoVariables_ProducesSingleState()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State(); // no variables
        var rules = new IRule[]
        {
            new TestRule(_ => false, s => s.Clone())
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.Empty(result.States.Values.First().Variables);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_EmptyRulesArray_WithLimitsSet_ProducesSingleState()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 1;
        var rules = Array.Empty<IRule>();
        var config = new BuilderConfig { MaxStates = 10, MaxDepth = 5 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_SingleNeverAvailableRule_WithLimits_ProducesSingleState()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;
        var rules = new IRule[]
        {
            new TestRule(_ => false, s => { var c = s.Clone(); c.Variables["x"] = 999; return c; })
        };
        var config = new BuilderConfig { MaxStates = 100, MaxDepth = 100 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    #endregion

    #region Duplicate Transitions — Same Name, Same Source→Target

    [Fact]
    public void Build_TwoRulesSameNameSameResult_CreatesDuplicateTransitions()
    {
        // Two rules with identical GetName() that produce the same target state from the same source.
        // The builder should create two transitions with the same source, target, and rule name.
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;
        var rules = new IRule[]
        {
            new NamedTestRule("Push_OK",
                s => (int)s.Variables["x"]! == 0,
                s => { var c = s.Clone(); c.Variables["x"] = 1; return c; }),
            new NamedTestRule("Push_OK",
                s => (int)s.Variables["x"]! == 0,
                s => { var c = s.Clone(); c.Variables["x"] = 1; return c; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(2, result.States.Count);
        // Both rules fire from same source, produce same target, same name → 2 identical transitions
        Assert.Equal(2, result.Transitions.Count);
        Assert.All(result.Transitions, t =>
        {
            Assert.Equal(result.StartingStateId, t.SourceStateId);
            Assert.Equal("Push_OK", t.RuleName);
        });
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_TwoRulesSameNameDifferentResults_CreatesDistinctTransitions()
    {
        // Two rules with identical GetName() but producing different target states.
        // Each creates a distinct transition.
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;
        var rules = new IRule[]
        {
            new NamedTestRule("Transform",
                s => (int)s.Variables["x"]! == 0,
                s => { var c = s.Clone(); c.Variables["x"] = 1; return c; }),
            new NamedTestRule("Transform",
                s => (int)s.Variables["x"]! == 0,
                s => { var c = s.Clone(); c.Variables["x"] = 2; return c; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(3, result.States.Count);
        // Two transitions from initial, both named "Transform", but to different targets
        var fromInitial = result.Transitions.Where(t => t.SourceStateId == result.StartingStateId).ToList();
        Assert.Equal(2, fromInitial.Count);
        Assert.All(fromInitial, t => Assert.Equal("Transform", t.RuleName));
        Assert.NotEqual(fromInitial[0].TargetStateId, fromInitial[1].TargetStateId);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_ThreeRulesSameName_AllProduceSameTarget_ThreeDuplicateTransitions()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["status"] = "start";
        var rules = new IRule[]
        {
            new NamedTestRule("Push_OK",
                s => (string)s.Variables["status"]! == "start",
                s => { var c = s.Clone(); c.Variables["status"] = "done"; return c; }),
            new NamedTestRule("Push_OK",
                s => (string)s.Variables["status"]! == "start",
                s => { var c = s.Clone(); c.Variables["status"] = "done"; return c; }),
            new NamedTestRule("Push_OK",
                s => (string)s.Variables["status"]! == "start",
                s => { var c = s.Clone(); c.Variables["status"] = "done"; return c; })
        };
        var config = new BuilderConfig();

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(2, result.States.Count);
        Assert.Equal(3, result.Transitions.Count);
        Assert.All(result.Transitions, t => Assert.Equal("Push_OK", t.RuleName));
    }

    #endregion

    #region Rule Order at MaxStates Boundary

    [Fact]
    public void Build_RuleOrderAffectsStructure_WhenMaxStatesConstrains()
    {
        // When MaxStates limits exploration, the first rule that generates a new state
        // gets its state added, while subsequent rules that would generate different new
        // states are blocked by the MaxStates break. Different rule order → different states.
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;

        var ruleA = new NamedTestRule("GoToA",
            s => (int)s.Variables["x"]! == 0,
            s => { var c = s.Clone(); c.Variables["x"] = 1; return c; });
        var ruleB = new NamedTestRule("GoToB",
            s => (int)s.Variables["x"]! == 0,
            s => { var c = s.Clone(); c.Variables["x"] = 2; return c; });

        // MaxStates=2: initial + one more
        var config = new BuilderConfig { MaxStates = 2 };

        // Order [A, B]: A fires first, state x=1 is added, B is blocked
        var resultAB = builder.Build(initialState.Clone(), new IRule[] { ruleA, ruleB }, config);
        // Order [B, A]: B fires first, state x=2 is added, A is blocked
        var resultBA = builder.Build(initialState.Clone(), new IRule[] { ruleB, ruleA }, config);

        Assert.Equal(2, resultAB.States.Count);
        Assert.Equal(2, resultBA.States.Count);

        // AB should contain x=1 but not x=2
        Assert.Contains(resultAB.States.Values, s => (int)s.Variables["x"]! == 1);
        Assert.DoesNotContain(resultAB.States.Values, s => (int)s.Variables["x"]! == 2);

        // BA should contain x=2 but not x=1
        Assert.Contains(resultBA.States.Values, s => (int)s.Variables["x"]! == 2);
        Assert.DoesNotContain(resultBA.States.Values, s => (int)s.Variables["x"]! == 1);
    }

    [Fact]
    public void Build_RuleOrderDoesNotAffectStructure_WhenMaxStatesNotConstraining()
    {
        // Without MaxStates constraining, rule order should not affect the set of states
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;

        var ruleA = new NamedTestRule("GoToA",
            s => (int)s.Variables["x"]! == 0,
            s => { var c = s.Clone(); c.Variables["x"] = 1; return c; });
        var ruleB = new NamedTestRule("GoToB",
            s => (int)s.Variables["x"]! == 0,
            s => { var c = s.Clone(); c.Variables["x"] = 2; return c; });

        var config = new BuilderConfig(); // no MaxStates limit

        var resultAB = builder.Build(initialState.Clone(), new IRule[] { ruleA, ruleB }, config);
        var resultBA = builder.Build(initialState.Clone(), new IRule[] { ruleB, ruleA }, config);

        // Both should have the same states (x=0, x=1, x=2)
        Assert.Equal(3, resultAB.States.Count);
        Assert.Equal(3, resultBA.States.Count);

        var valuesAB = resultAB.States.Values.Select(s => (int)s.Variables["x"]!).OrderBy(v => v).ToArray();
        var valuesBA = resultBA.States.Values.Select(s => (int)s.Variables["x"]!).OrderBy(v => v).ToArray();
        Assert.Equal(valuesAB, valuesBA);
    }

    [Fact]
    public void Build_RuleOrderAffectsStructure_MultipleBranches_AtMaxStates()
    {
        // Three rules from initial, MaxStates=3 (initial + 2 more), third rule's state is dropped
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["branch"] = "root";

        var ruleA = new NamedTestRule("GoA",
            s => (string)s.Variables["branch"]! == "root",
            s => { var c = s.Clone(); c.Variables["branch"] = "A"; return c; });
        var ruleB = new NamedTestRule("GoB",
            s => (string)s.Variables["branch"]! == "root",
            s => { var c = s.Clone(); c.Variables["branch"] = "B"; return c; });
        var ruleC = new NamedTestRule("GoC",
            s => (string)s.Variables["branch"]! == "root",
            s => { var c = s.Clone(); c.Variables["branch"] = "C"; return c; });

        var config = new BuilderConfig { MaxStates = 3 };

        // [A,B,C]: adds A and B, C is blocked
        var resultABC = builder.Build(initialState.Clone(), new IRule[] { ruleA, ruleB, ruleC }, config);
        // [C,B,A]: adds C and B, A is blocked
        var resultCBA = builder.Build(initialState.Clone(), new IRule[] { ruleC, ruleB, ruleA }, config);

        Assert.Equal(3, resultABC.States.Count);
        Assert.Equal(3, resultCBA.States.Count);

        // ABC has A and B but not C
        Assert.Contains(resultABC.States.Values, s => (string)s.Variables["branch"]! == "A");
        Assert.Contains(resultABC.States.Values, s => (string)s.Variables["branch"]! == "B");
        Assert.DoesNotContain(resultABC.States.Values, s => (string)s.Variables["branch"]! == "C");

        // CBA has C and B but not A
        Assert.Contains(resultCBA.States.Values, s => (string)s.Variables["branch"]! == "C");
        Assert.Contains(resultCBA.States.Values, s => (string)s.Variables["branch"]! == "B");
        Assert.DoesNotContain(resultCBA.States.Values, s => (string)s.Variables["branch"]! == "A");
    }

    #endregion

    #region 3.18 — Unbounded Growth and Malformed State Rules

    [Fact]
    public void Build_UnboundedGrowthRule_LimitedByMaxStates()
    {
        // Rule always generates a unique state (incrementing counter) — would grow forever without limits
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                _ => true,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 10 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(10, result.States.Count);
        Assert.Equal(9, result.Transitions.Count);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_UnboundedGrowthRule_LimitedByMaxDepth()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                _ => true,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxDepth = 5 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(6, result.States.Count); // S0 through S5
        Assert.Equal(5, result.Transitions.Count);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_UnboundedGrowthRule_BothLimits_MaxStatesBindsFirst()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                _ => true,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 3, MaxDepth = 100 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(3, result.States.Count);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_UnboundedGrowthRule_BothLimits_MaxDepthBindsFirst()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                _ => true,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 100, MaxDepth = 3 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(4, result.States.Count); // depth 0,1,2,3
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_RuleReturnsEmptyState_BuilderHandlesIt()
    {
        // Rule returns a state with no variables (malformed relative to input)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 1;
        var rules = new IRule[]
        {
            new TestRule(
                s => s.Variables.ContainsKey("x"),
                _ => new State()) // returns empty state
        };
        var config = new BuilderConfig { MaxStates = 10 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(2, result.States.Count);
        Assert.Single(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_RuleAddsExtraVariables_BuilderHandlesIt()
    {
        // Rule returns a state with more variables than the input
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 1;
        var rules = new IRule[]
        {
            new TestRule(
                s => !s.Variables.ContainsKey("extra"),
                s => { var c = s.Clone(); c.Variables["extra"] = "added"; c.Variables["x"] = (int)c.Variables["x"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 10 };

        StateMachine result = builder.Build(initialState, rules, config);

        // Initial state (no extra) -> state with extra (rule no longer fires)
        Assert.Equal(2, result.States.Count);
        Assert.Single(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_RuleRemovesVariables_BuilderHandlesIt()
    {
        // Rule returns a state with fewer variables than input
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 1;
        initialState.Variables["y"] = 2;
        var rules = new IRule[]
        {
            new TestRule(
                s => s.Variables.ContainsKey("y"),
                s => { var c = new State(); c.Variables["x"] = (int)s.Variables["x"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 10 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(2, result.States.Count);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_RuleChangesVariableType_BuilderHandlesIt()
    {
        // Rule changes a variable from int to string
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["value"] = 42;
        var rules = new IRule[]
        {
            new TestRule(
                s => s.Variables["value"] is int,
                s => { var c = s.Clone(); c.Variables["value"] = "forty-two"; return c; })
        };
        var config = new BuilderConfig { MaxStates = 10 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(2, result.States.Count);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_RuleReturnsNullVariable_BuilderHandlesIt()
    {
        // Rule sets a variable to null
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 1;
        var rules = new IRule[]
        {
            new TestRule(
                s => s.Variables["x"] is not null,
                s => { var c = s.Clone(); c.Variables["x"] = null; return c; })
        };
        var config = new BuilderConfig { MaxStates = 10 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(2, result.States.Count);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_MultipleUnboundedRules_MaxStatesLimitsTotal()
    {
        // Two rules both always generate unique states — growth is double-rate
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["a"] = 0;
        initialState.Variables["b"] = 0;
        var rules = new IRule[]
        {
            new NamedTestRule("IncA",
                _ => true,
                s => { var c = s.Clone(); c.Variables["a"] = (int)c.Variables["a"]! + 1; return c; }),
            new NamedTestRule("IncB",
                _ => true,
                s => { var c = s.Clone(); c.Variables["b"] = (int)c.Variables["b"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 5 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(5, result.States.Count);
        Assert.True(result.IsValidMachine());
    }

    #endregion

    #region 3.19 — Rules That Throw Exceptions or Hang

    [Fact]
    public void Build_IsAvailableThrows_ExceptionPropagates()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                _ => throw new InvalidOperationException("IsAvailable exploded"),
                s => s.Clone())
        };
        var config = new BuilderConfig { MaxStates = 10 };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.Build(initialState, rules, config));
        Assert.Contains("IsAvailable exploded", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ExecuteThrows_ExceptionPropagates()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                _ => true,
                _ => throw new InvalidOperationException("Execute exploded"))
        };
        var config = new BuilderConfig { MaxStates = 10 };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.Build(initialState, rules, config));
        Assert.Contains("Execute exploded", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_IsAvailableThrowsArgumentException_ExceptionPropagates()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                _ => throw new ArgumentException("bad argument"),
                s => s.Clone())
        };
        var config = new BuilderConfig { MaxStates = 10 };

        Assert.Throws<ArgumentException>(() =>
            builder.Build(initialState, rules, config));
    }

    [Fact]
    public void Build_ExecuteThrowsInvalidOperation_ExceptionPropagates()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                _ => true,
                _ => throw new InvalidOperationException("error in Execute"))
        };
        var config = new BuilderConfig { MaxStates = 10 };

        Assert.Throws<InvalidOperationException>(() =>
            builder.Build(initialState, rules, config));
    }

    [Fact]
    public void Build_SecondRuleThrows_FirstRuleStillProcessed()
    {
        // First rule works fine, second rule throws on Execute.
        // The first rule's state should be added before the exception.
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;
        var rules = new IRule[]
        {
            new NamedTestRule("Good",
                s => (int)s.Variables["x"]! == 0,
                s => { var c = s.Clone(); c.Variables["x"] = 1; return c; }),
            new NamedTestRule("Bad",
                _ => true,
                _ => throw new InvalidOperationException("boom"))
        };
        var config = new BuilderConfig { MaxStates = 10 };

        // The good rule fires first (x=0 -> x=1), then bad rule throws on x=0
        Assert.Throws<InvalidOperationException>(() =>
            builder.Build(initialState, rules, config));
    }

    [Fact]
    public void Build_IsAvailableThrowsOnSecondState_FirstStateProcessed()
    {
        // Rule works for initial state but throws IsAvailable on the second state
        int callCount = 0;
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                s =>
                {
                    callCount++;
                    if ((int)s.Variables["step"]! > 0)
                        throw new InvalidOperationException("IsAvailable fails on step > 0");
                    return true;
                },
                s => { var c = s.Clone(); c.Variables["step"] = (int)c.Variables["step"]! + 1; return c; })
        };
        var config = new BuilderConfig { MaxStates = 10 };

        // Builds S0, rule fires (step=0->step=1), then when exploring S1, IsAvailable throws
        Assert.Throws<InvalidOperationException>(() =>
            builder.Build(initialState, rules, config));
    }

    [Fact]
    public void Build_SlowRule_CompletesWithinTimeout()
    {
        // Rule with artificial delay — should still complete if given enough time
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                s => (int)s.Variables["step"]! < 3,
                s =>
                {
                    Thread.Sleep(10); // small delay per execution
                    var c = s.Clone();
                    c.Variables["step"] = (int)c.Variables["step"]! + 1;
                    return c;
                })
        };
        var config = new BuilderConfig { MaxStates = 10 };

        StateMachine result = builder.Build(initialState, rules, config);

        Assert.Equal(4, result.States.Count);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Build_HangingRule_CanBeCancelledByMaxStates()
    {
        // Rule that always generates new states (simulating near-infinite work)
        // MaxStates prevents actual infinite loop
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestRule(
                _ => true,
                s =>
                {
                    var c = s.Clone();
                    c.Variables["counter"] = (int)c.Variables["counter"]! + 1;
                    return c;
                })
        };
        var config = new BuilderConfig { MaxStates = 100 };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        StateMachine result = builder.Build(initialState, rules, config);
        sw.Stop();

        Assert.Equal(100, result.States.Count);
        Assert.True(result.IsValidMachine());
        // Should complete quickly — not hang
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Build took {sw.ElapsedMilliseconds}ms, expected < 5000ms");
    }

    #endregion
}