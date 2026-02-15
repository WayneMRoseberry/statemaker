namespace StateMaker.Tests;

public class StateMachineShapeTests
{
    private sealed class AlwaysFalseRule : IRule
    {
        public bool IsAvailable(State state) => false;
        public State Execute(State state) => state.Clone();
    }

    private sealed class CloneStateRule : IRule
    {
        public bool IsAvailable(State state) => true;
        public State Execute(State state) => state.Clone();
    }

    private sealed class IncrementRule : IRule
    {
        private readonly string _variable;
        private readonly int _maxValue;

        public IncrementRule(string variable, int maxValue)
        {
            _variable = variable;
            _maxValue = maxValue;
        }

        public bool IsAvailable(State state) =>
            state.Variables.ContainsKey(_variable) && (int)state.Variables[_variable]! < _maxValue;

        public State Execute(State state)
        {
            var clone = state.Clone();
            clone.Variables[_variable] = (int)clone.Variables[_variable]! + 1;
            return clone;
        }
    }

    private sealed class ModularCycleRule : IRule
    {
        private readonly string _variable;
        private readonly int _cycleLength;

        public ModularCycleRule(string variable, int cycleLength)
        {
            _variable = variable;
            _cycleLength = cycleLength;
        }

        public bool IsAvailable(State state) =>
            state.Variables.ContainsKey(_variable);

        public State Execute(State state)
        {
            var clone = state.Clone();
            clone.Variables[_variable] = ((int)clone.Variables[_variable]! + 1) % _cycleLength;
            return clone;
        }
    }

    private sealed class StringCycleRule : IRule
    {
        private readonly string _variable;
        private readonly string[] _values;

        public StringCycleRule(string variable, string[] values)
        {
            _variable = variable;
            _values = values;
        }

        public bool IsAvailable(State state) =>
            state.Variables.ContainsKey(_variable);

        public State Execute(State state)
        {
            var clone = state.Clone();
            var current = (string)clone.Variables[_variable]!;
            int index = Array.IndexOf(_values, current);
            clone.Variables[_variable] = _values[(index + 1) % _values.Length];
            return clone;
        }
    }

    private sealed class StringChainRule : IRule
    {
        private readonly string _variable;
        private readonly string[] _sequence;

        public StringChainRule(string variable, string[] sequence)
        {
            _variable = variable;
            _sequence = sequence;
        }

        public bool IsAvailable(State state)
        {
            if (!state.Variables.TryGetValue(_variable, out var value))
                return false;
            var current = (string)value!;
            int index = Array.IndexOf(_sequence, current);
            return index >= 0 && index < _sequence.Length - 1;
        }

        public State Execute(State state)
        {
            var clone = state.Clone();
            var current = (string)clone.Variables[_variable]!;
            int index = Array.IndexOf(_sequence, current);
            clone.Variables[_variable] = _sequence[index + 1];
            return clone;
        }
    }

    private sealed class FuncRule : IRule
    {
        private readonly Func<State, bool> _isAvailable;
        private readonly Func<State, State> _execute;

        public FuncRule(Func<State, bool> isAvailable, Func<State, State> execute)
        {
            _isAvailable = isAvailable;
            _execute = execute;
        }

        public bool IsAvailable(State state) => _isAvailable(state);
        public State Execute(State state) => _execute(state);
    }

    private sealed class ChainThenCycleRule : IRule
    {
        private readonly string _variable;
        private readonly int _chainLength;
        private readonly int _cycleLength;

        public ChainThenCycleRule(string variable, int chainLength, int cycleLength)
        {
            _variable = variable;
            _chainLength = chainLength;
            _cycleLength = cycleLength;
        }

        public bool IsAvailable(State state) =>
            state.Variables.ContainsKey(_variable) &&
            (int)state.Variables[_variable]! >= 0 &&
            (int)state.Variables[_variable]! < _chainLength + _cycleLength;

        public State Execute(State state)
        {
            var clone = state.Clone();
            int value = (int)clone.Variables[_variable]!;
            if (value < _chainLength + _cycleLength - 1)
                clone.Variables[_variable] = value + 1;
            else
                clone.Variables[_variable] = _chainLength;
            return clone;
        }
    }

    #region Helper Methods

    private static void AssertChainShape(StateMachine machine, int chainLength)
    {
        Assert.Equal(chainLength + 1, machine.States.Count);
        Assert.Equal(chainLength, machine.Transitions.Count);
        Assert.True(machine.IsValidMachine());

        // Each state except the last has exactly one outgoing transition
        // Each state except the first has exactly one incoming transition
        var stateIds = machine.States.Keys.ToList();
        foreach (var stateId in stateIds)
        {
            int outDegree = machine.Transitions.Count(t => t.SourceStateId == stateId);
            int inDegree = machine.Transitions.Count(t => t.TargetStateId == stateId);

            if (stateId == machine.StartingStateId)
            {
                Assert.Equal(1, outDegree);
                Assert.Equal(0, inDegree);
            }
            else
            {
                Assert.Equal(1, inDegree);
                // Last state has 0 outgoing, all others have 1
                Assert.True(outDegree <= 1);
            }
        }

        // Exactly one state has no outgoing transitions (the tail)
        int tailCount = stateIds.Count(id => machine.Transitions.All(t => t.SourceStateId != id));
        Assert.Equal(1, tailCount);

        // No state appears as a target more than once (no convergence)
        var targetCounts = machine.Transitions.GroupBy(t => t.TargetStateId)
            .Select(g => g.Count());
        Assert.All(targetCounts, c => Assert.Equal(1, c));

        // No state appears as a source more than once (no branching)
        var sourceCounts = machine.Transitions.GroupBy(t => t.SourceStateId)
            .Select(g => g.Count());
        Assert.All(sourceCounts, c => Assert.Equal(1, c));
    }

    private static void AssertCycleShape(StateMachine machine, int cycleLength)
    {
        Assert.Equal(cycleLength, machine.States.Count);
        Assert.Equal(cycleLength, machine.Transitions.Count);
        Assert.True(machine.IsValidMachine());

        // Every state has exactly one outgoing and one incoming transition
        var stateIds = machine.States.Keys.ToList();
        foreach (var stateId in stateIds)
        {
            int outDegree = machine.Transitions.Count(t => t.SourceStateId == stateId);
            int inDegree = machine.Transitions.Count(t => t.TargetStateId == stateId);
            Assert.Equal(1, outDegree);
            Assert.Equal(1, inDegree);
        }

        // Exactly one transition has TargetStateId == StartingStateId (the back-edge)
        int backEdgeCount = machine.Transitions.Count(t => t.TargetStateId == machine.StartingStateId);
        Assert.Equal(1, backEdgeCount);
    }

    private static void AssertChainThenCycleShape(StateMachine machine, int chainLength, int cycleLength)
    {
        Assert.Equal(chainLength + cycleLength, machine.States.Count);
        Assert.Equal(chainLength + cycleLength, machine.Transitions.Count);
        Assert.True(machine.IsValidMachine());

        // Initial state has outDegree == 1, inDegree == 0
        int startOut = machine.Transitions.Count(t => t.SourceStateId == machine.StartingStateId);
        int startIn = machine.Transitions.Count(t => t.TargetStateId == machine.StartingStateId);
        Assert.Equal(1, startOut);
        Assert.Equal(0, startIn);

        // No transition targets the initial state (back-edge goes to cycle entry, not S0)
        Assert.DoesNotContain(machine.Transitions, t => t.TargetStateId == machine.StartingStateId);

        // Exactly one state has inDegree == 2 (the cycle entry state)
        var stateIds = machine.States.Keys.ToList();
        int inDegree2Count = stateIds.Count(id =>
            machine.Transitions.Count(t => t.TargetStateId == id) == 2);
        Assert.Equal(1, inDegree2Count);
    }

    private static void AssertAllStatesReachable(StateMachine machine)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(machine.StartingStateId!);
        visited.Add(machine.StartingStateId!);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var transition in machine.Transitions.Where(t => t.SourceStateId == current))
            {
                if (visited.Add(transition.TargetStateId))
                    queue.Enqueue(transition.TargetStateId);
            }
        }

        Assert.Equal(machine.States.Count, visited.Count);
    }

    private static FuncRule CycleInRange(int rangeStart, int cycleLen) => new(
        s => s.Variables.ContainsKey("step") &&
             (int)s.Variables["step"]! >= rangeStart &&
             (int)s.Variables["step"]! < rangeStart + cycleLen,
        s =>
        {
            var c = s.Clone();
            c.Variables["step"] = rangeStart + ((int)c.Variables["step"]! - rangeStart + 1) % cycleLen;
            return c;
        });

    private static FuncRule TransitionAt(int fromValue, int toValue) => new(
        s => s.Variables.ContainsKey("step") && (int)s.Variables["step"]! == fromValue,
        s =>
        {
            var c = s.Clone();
            c.Variables["step"] = toValue;
            return c;
        });

    private static FuncRule AppendPathRule(string suffix, int maxPathLength) => new(
        s => s.Variables.ContainsKey("path") && ((string)s.Variables["path"]!).Length < maxPathLength,
        s =>
        {
            var c = s.Clone();
            c.Variables["path"] = (string)c.Variables["path"]! + suffix;
            return c;
        });

    private static FuncRule LevelTransition(int fromLevel, int toLevel, int toValue) => new(
        s => s.Variables.ContainsKey("level") && (int)s.Variables["level"]! == fromLevel,
        s =>
        {
            var c = s.Clone();
            c.Variables["level"] = toLevel;
            c.Variables["value"] = toValue;
            return c;
        });

    private static void AssertTreeShape(StateMachine machine, int expectedStates, int expectedTransitions)
    {
        Assert.Equal(expectedStates, machine.States.Count);
        Assert.Equal(expectedTransitions, machine.Transitions.Count);
        AssertAllStatesReachable(machine);
        Assert.True(machine.IsValidMachine());
    }

    private static void AssertNoCycles(StateMachine machine)
    {
        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        void CheckNoCycle(string stateId)
        {
            visited.Add(stateId);
            inStack.Add(stateId);
            foreach (var t in machine.Transitions.Where(t => t.SourceStateId == stateId))
            {
                if (inStack.Contains(t.TargetStateId))
                    Assert.Fail($"Cycle detected: transition from {t.SourceStateId} to {t.TargetStateId}");
                if (!visited.Contains(t.TargetStateId))
                    CheckNoCycle(t.TargetStateId);
            }
            inStack.Remove(stateId);
        }

        CheckNoCycle(machine.StartingStateId!);
    }

    private static void AssertDiamondShape(StateMachine machine, int branchCount, int expectedStates, int expectedTransitions)
    {
        Assert.Equal(expectedStates, machine.States.Count);
        Assert.Equal(expectedTransitions, machine.Transitions.Count);
        AssertAllStatesReachable(machine);
        Assert.True(machine.IsValidMachine());

        // At least one convergence point with inDegree >= branchCount
        var convergencePoints = machine.States.Keys
            .Where(id => machine.Transitions.Count(t => t.TargetStateId == id) >= branchCount)
            .ToList();
        Assert.NotEmpty(convergencePoints);
    }

    private static void AssertFullyConnectedGraph(StateMachine machine, int nodeCount)
    {
        Assert.Equal(nodeCount, machine.States.Count);
        Assert.Equal(nodeCount * (nodeCount - 1), machine.Transitions.Count);
        AssertAllStatesReachable(machine);
        Assert.True(machine.IsValidMachine());

        // Every state has outDegree == K-1 and inDegree == K-1
        foreach (var stateId in machine.States.Keys)
        {
            int outDegree = machine.Transitions.Count(t => t.SourceStateId == stateId);
            int inDegree = machine.Transitions.Count(t => t.TargetStateId == stateId);
            Assert.Equal(nodeCount - 1, outDegree);
            Assert.Equal(nodeCount - 1, inDegree);
        }
    }

    private static FuncRule ModularOffsetRule(int offset, int modulus) => new(
        s => s.Variables.ContainsKey("step"),
        s =>
        {
            var c = s.Clone();
            c.Variables["step"] = ((int)c.Variables["step"]! + offset) % modulus;
            return c;
        });

    #endregion

    #region Single State Shape Tests

    [Fact]
    public void SingleState_NoRules_ProducesSingleStateNoTransitions()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;

        StateMachine result = builder.Build(initialState, Array.Empty<IRule>(), new BuilderConfig());

        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void SingleState_OneAlwaysFalseRule_ProducesSingleStateNoTransitions()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;
        var rules = new IRule[] { new AlwaysFalseRule() };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void SingleState_MultipleAlwaysFalseRules_ProducesSingleStateNoTransitions()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 0;
        var rules = new IRule[] { new AlwaysFalseRule(), new AlwaysFalseRule(), new AlwaysFalseRule() };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void SingleState_CloneRule_ProducesSingleStateWithSelfLoop()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["x"] = 42;
        var rules = new IRule[] { new CloneStateRule() };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Single(result.States);
        Assert.Single(result.Transitions);
        Assert.Equal(result.StartingStateId, result.Transitions[0].SourceStateId);
        Assert.Equal(result.StartingStateId, result.Transitions[0].TargetStateId);
        Assert.True(result.IsValidMachine());
    }

    [Theory]
    [InlineData("status", "pending")]
    [InlineData("count", 0)]
    [InlineData("active", true)]
    [InlineData("rate", 3.14f)]
    public void SingleState_VariousVariableTypes_ProducesSingleState(string key, object value)
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables[key] = value;
        var rules = new IRule[] { new AlwaysFalseRule() };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void SingleState_EmptyState_ProducesSingleState()
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        var rules = new IRule[] { new AlwaysFalseRule() };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Single(result.States);
        Assert.Empty(result.Transitions);
        Assert.True(result.IsValidMachine());
    }

    #endregion

    #region Chain Shape Tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void Chain_IncrementingInt_ProducesChainOfExpectedLength(int chainLength)
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[] { new IncrementRule("step", chainLength) };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertChainShape(result, chainLength);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void Chain_StringSequence_ProducesChainOfExpectedLength(int chainLength)
    {
        var builder = new StateMachineBuilder();
        var values = Enumerable.Range(0, chainLength + 1).Select(i => $"state_{i}").ToArray();
        var initialState = new State();
        initialState.Variables["status"] = values[0];
        var rules = new IRule[] { new StringChainRule("status", values) };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertChainShape(result, chainLength);
    }

    #endregion

    #region Cycle Shape Tests

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void Cycle_ModularArithmetic_ProducesCycleOfExpectedLength(int cycleLength)
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["value"] = 0;
        var rules = new IRule[] { new ModularCycleRule("value", cycleLength) };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertCycleShape(result, cycleLength);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void Cycle_StringValues_ProducesCycleOfExpectedLength(int cycleLength)
    {
        var builder = new StateMachineBuilder();
        var values = Enumerable.Range(0, cycleLength).Select(i => $"phase_{i}").ToArray();
        var initialState = new State();
        initialState.Variables["phase"] = values[0];
        var rules = new IRule[] { new StringCycleRule("phase", values) };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertCycleShape(result, cycleLength);
    }

    #endregion

    #region Complex Cycle — Chain Then Cycle

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(5, 2)]
    [InlineData(1, 5)]
    public void ChainThenCycle_ProducesExpectedShape(int chainLength, int cycleLength)
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[] { new ChainThenCycleRule("step", chainLength, cycleLength) };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertChainThenCycleShape(result, chainLength, cycleLength);
        AssertAllStatesReachable(result);
    }

    #endregion

    #region Complex Cycle — Start Points

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    public void CycleStartPoint_BackEdgeTargetsNonStartState(int chainLength, int cycleLength)
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[] { new ChainThenCycleRule("step", chainLength, cycleLength) };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // The back-edge should NOT target the starting state
        Assert.DoesNotContain(result.Transitions, t => t.TargetStateId == result.StartingStateId);

        // The back-edge target has inDegree == 2
        var backEdgeTargets = result.States.Keys
            .Where(id => result.Transitions.Count(t => t.TargetStateId == id) == 2)
            .ToList();
        Assert.Single(backEdgeTargets);

        Assert.True(result.IsValidMachine());
    }

    #endregion

    #region Complex Cycle — Nested Cycles

    [Fact]
    public void NestedCycle_TwoAdjacentCyclesSharingState()
    {
        // Outer cycle: 0→1→0, branch from 1 to inner cycle: 100→101→100
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            CycleInRange(0, 2),
            TransitionAt(1, 100),
            CycleInRange(100, 2),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(4, result.States.Count);
        Assert.Equal(5, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void NestedCycle_SequentialCyclesChainToCycleAExitToCycleB()
    {
        // Chain 0→1, cycle A: 1→2→1, exit from 2 to cycle B: 100→101→100
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new ChainThenCycleRule("step", 1, 2),
            TransitionAt(2, 100),
            CycleInRange(100, 2),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(5, result.States.Count);
        Assert.Equal(6, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void NestedCycle_TwoIndependentCyclesFromBranchPoint()
    {
        // S0 branches to cycle A (10→11→10) and cycle B (20→21→20)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10),
            TransitionAt(0, 20),
            CycleInRange(10, 2),
            CycleInRange(20, 2),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(5, result.States.Count);
        Assert.Equal(6, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void NestedCycle_OuterCycle3InnerCycle2()
    {
        // Outer cycle: 0→1→2→0, branch from 1 to inner: 100→101→100
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            CycleInRange(0, 3),
            TransitionAt(1, 100),
            CycleInRange(100, 2),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(5, result.States.Count);
        Assert.Equal(6, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void NestedCycle_OuterCycle2InnerCycle3()
    {
        // Outer cycle: 0→1→0, branch from 0 to inner: 100→101→102→100
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            CycleInRange(0, 2),
            TransitionAt(0, 100),
            CycleInRange(100, 3),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(5, result.States.Count);
        Assert.Equal(6, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    #endregion

    #region Complex Cycle — Optional Exits

    [Fact]
    public void CycleWithExit_Cycle2OneExitLength1()
    {
        // Cycle: 0→1→0, exit from 0 to 100
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            CycleInRange(0, 2),
            TransitionAt(0, 100),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(3, result.States.Count);
        Assert.Equal(3, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void CycleWithExit_Cycle3OneExitLength1()
    {
        // Cycle: 0→1→2→0, exit from 1 to 100
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            CycleInRange(0, 3),
            TransitionAt(1, 100),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(4, result.States.Count);
        Assert.Equal(4, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void CycleWithExit_Cycle3ExitChainLength3()
    {
        // Cycle: 0→1→2→0, exit from 1 to chain: 100→101→102
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            CycleInRange(0, 3),
            TransitionAt(1, 100),
            TransitionAt(100, 101),
            TransitionAt(101, 102),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(6, result.States.Count);
        Assert.Equal(6, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void CycleWithExit_Cycle2ExitsFromEveryState()
    {
        // Cycle: 0→1→0, exits: 0→100, 1→200
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            CycleInRange(0, 2),
            TransitionAt(0, 100),
            TransitionAt(1, 200),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(4, result.States.Count);
        Assert.Equal(4, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void CycleWithExit_Cycle3TwoExitsFromSameState()
    {
        // Cycle: 0→1→2→0, exits from 1: 1→100, 1→200
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            CycleInRange(0, 3),
            TransitionAt(1, 100),
            TransitionAt(1, 200),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(5, result.States.Count);
        Assert.Equal(5, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    #endregion

    #region Branch — Varying Peer Count

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void Branch_VaryingPeerCount_ProducesCorrectFanOut(int peerCount)
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = Enumerable.Range(1, peerCount)
            .Select(i => (IRule)TransitionAt(0, i))
            .ToArray();

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertTreeShape(result, peerCount + 1, peerCount);
        AssertNoCycles(result);

        // Root has outDegree == peerCount
        int rootOutDegree = result.Transitions.Count(t => t.SourceStateId == result.StartingStateId);
        Assert.Equal(peerCount, rootOutDegree);

        // All children: outDegree == 0, inDegree == 1
        foreach (var stateId in result.States.Keys.Where(id => id != result.StartingStateId))
        {
            Assert.Equal(0, result.Transitions.Count(t => t.SourceStateId == stateId));
            Assert.Equal(1, result.Transitions.Count(t => t.TargetStateId == stateId));
        }
    }

    #endregion

    #region Branch — Complete Binary Trees (Depth)

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Branch_CompleteBinaryTree_ProducesCorrectDepth(int depth)
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["path"] = "";
        var rules = new IRule[] { AppendPathRule("L", depth), AppendPathRule("R", depth) };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        int expectedStates = (1 << (depth + 1)) - 1; // 2^(depth+1) - 1
        int expectedTransitions = expectedStates - 1;
        AssertTreeShape(result, expectedStates, expectedTransitions);
        AssertNoCycles(result);

        // Root has outDegree == 2, inDegree == 0
        Assert.Equal(2, result.Transitions.Count(t => t.SourceStateId == result.StartingStateId));
        Assert.Equal(0, result.Transitions.Count(t => t.TargetStateId == result.StartingStateId));

        // No state has inDegree > 1 (no convergence in a tree)
        foreach (var stateId in result.States.Keys)
        {
            int inDegree = result.Transitions.Count(t => t.TargetStateId == stateId);
            Assert.True(inDegree <= 1, $"State {stateId} has inDegree {inDegree}, expected <= 1");
        }
    }

    #endregion

    #region Branch — Varying Breadth

    [Theory]
    [InlineData(3, 2)]  // 1+3+9 = 13 states
    [InlineData(2, 3)]  // 1+2+4+8 = 15 states
    [InlineData(4, 2)]  // 1+4+16 = 21 states
    public void Branch_UniformTree_ProducesCorrectBreadthAndDepth(int breadth, int depth)
    {
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["path"] = "";
        var rules = Enumerable.Range(0, breadth)
            .Select(i => (IRule)AppendPathRule(i.ToString(System.Globalization.CultureInfo.InvariantCulture), depth))
            .ToArray();

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        int expectedStates = 0;
        for (int d = 0; d <= depth; d++)
            expectedStates += (int)Math.Pow(breadth, d);
        int expectedTransitions = expectedStates - 1;

        AssertTreeShape(result, expectedStates, expectedTransitions);
        AssertNoCycles(result);

        // Root has outDegree == breadth
        int rootOutDegree = result.Transitions.Count(t => t.SourceStateId == result.StartingStateId);
        Assert.Equal(breadth, rootOutDegree);

        // No convergence
        foreach (var stateId in result.States.Keys)
        {
            int inDegree = result.Transitions.Count(t => t.TargetStateId == stateId);
            Assert.True(inDegree <= 1, $"State {stateId} has inDegree {inDegree}, expected <= 1");
        }
    }

    #endregion

    #region Branch — Sub-branches as Trees

    [Fact]
    public void SubBranch_TwoIndependentChainsOfLength2()
    {
        // Root -> chain A (100->101->102), chain B (200->201->202)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 100), TransitionAt(0, 200),
            TransitionAt(100, 101), TransitionAt(101, 102),
            TransitionAt(200, 201), TransitionAt(201, 202),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertTreeShape(result, 7, 6);
        AssertNoCycles(result);

        // Root outDegree == 2
        Assert.Equal(2, result.Transitions.Count(t => t.SourceStateId == result.StartingStateId));
    }

    [Fact]
    public void SubBranch_TwoIndependentChainsOfDifferentLengths()
    {
        // Root -> chain A length 2 (100->101->102), chain B length 3 (200->201->202->203)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 100), TransitionAt(0, 200),
            TransitionAt(100, 101), TransitionAt(101, 102),
            TransitionAt(200, 201), TransitionAt(201, 202), TransitionAt(202, 203),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertTreeShape(result, 8, 7);
        AssertNoCycles(result);
    }

    [Fact]
    public void SubBranch_ThreeIndependentChainsOfLength1()
    {
        // Root -> 3 branches, each with 1 additional step
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 100), TransitionAt(0, 200), TransitionAt(0, 300),
            TransitionAt(100, 101), TransitionAt(200, 201), TransitionAt(300, 301),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertTreeShape(result, 7, 6);
        AssertNoCycles(result);
        Assert.Equal(3, result.Transitions.Count(t => t.SourceStateId == result.StartingStateId));
    }

    [Fact]
    public void SubBranch_TwoBinarySubTreesOfDepth1()
    {
        // Root -> branch A (100 -> 110, 120), branch B (200 -> 210, 220)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 100), TransitionAt(0, 200),
            TransitionAt(100, 110), TransitionAt(100, 120),
            TransitionAt(200, 210), TransitionAt(200, 220),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertTreeShape(result, 7, 6);
        AssertNoCycles(result);

        // Each sub-root has outDegree == 2
        var subRoots = result.Transitions
            .Where(t => t.SourceStateId == result.StartingStateId)
            .Select(t => t.TargetStateId)
            .ToList();
        Assert.Equal(2, subRoots.Count);
        foreach (var subRoot in subRoots)
        {
            Assert.Equal(2, result.Transitions.Count(t => t.SourceStateId == subRoot));
        }
    }

    #endregion

    #region Branch — Connected Sub-branches

    [Fact]
    public void ConnectedBranch_TwoRulesProduceSameChild()
    {
        // Two rules from root both produce {step: 1} -> deduplicated to 1 child
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            new FuncRule(
                s => (int)s.Variables["step"]! == 0,
                s => { var c = s.Clone(); c.Variables["step"] = 1; return c; }),
            new FuncRule(
                s => (int)s.Variables["step"]! == 0,
                s => { var c = s.Clone(); c.Variables["step"] = 1; return c; }),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(2, result.States.Count);
        Assert.Equal(2, result.Transitions.Count);
        // Both transitions target the same state
        var targets = result.Transitions.Select(t => t.TargetStateId).Distinct().ToList();
        Assert.Single(targets);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void ConnectedBranch_ThreeBranchesTwoProduceSameChild()
    {
        // 3 rules from root: two produce {step: 1}, one produces {step: 2}
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 1),
            new FuncRule(
                s => (int)s.Variables["step"]! == 0,
                s => { var c = s.Clone(); c.Variables["step"] = 1; return c; }),
            TransitionAt(0, 2),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(3, result.States.Count);
        Assert.Equal(3, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void ConnectedBranch_ChildrenProduceSameGrandchild()
    {
        // Root -> child A (step=10), child B (step=20)
        // Child A -> grandchild (step=100), Child B -> grandchild (step=100) — deduplicated
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10),
            TransitionAt(0, 20),
            TransitionAt(10, 100),
            TransitionAt(20, 100),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(4, result.States.Count);
        Assert.Equal(4, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());

        // The grandchild state has inDegree == 2
        var grandchildId = result.Transitions
            .GroupBy(t => t.TargetStateId)
            .First(g => g.Count() == 2)
            .Key;
        Assert.Equal(2, result.Transitions.Count(t => t.TargetStateId == grandchildId));
    }

    #endregion

    #region Branch — Fully Connected Branches

    [Fact]
    public void FullyConnected_2x2_FiveStates6Transitions()
    {
        // Root -> 2 L1 states, each L1 -> 2 L2 states (shared)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["level"] = 0;
        initialState.Variables["value"] = 0;
        var rules = new IRule[]
        {
            LevelTransition(0, 1, 10),
            LevelTransition(0, 1, 20),
            LevelTransition(1, 2, 100),
            LevelTransition(1, 2, 200),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(5, result.States.Count);
        Assert.Equal(6, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());

        // Root outDegree == 2
        Assert.Equal(2, result.Transitions.Count(t => t.SourceStateId == result.StartingStateId));

        // L2 states have inDegree == 2 (reached from both L1 states)
        var l2States = result.States.Keys
            .Where(id => result.Transitions.Count(t => t.TargetStateId == id) == 2)
            .ToList();
        Assert.Equal(2, l2States.Count);
    }

    [Fact]
    public void FullyConnected_2x3_SixStates8Transitions()
    {
        // Root -> 2 L1 states, each L1 -> 3 L2 states (shared)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["level"] = 0;
        initialState.Variables["value"] = 0;
        var rules = new IRule[]
        {
            LevelTransition(0, 1, 10),
            LevelTransition(0, 1, 20),
            LevelTransition(1, 2, 100),
            LevelTransition(1, 2, 200),
            LevelTransition(1, 2, 300),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(6, result.States.Count);
        Assert.Equal(8, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void FullyConnected_3x2_SixStates9Transitions()
    {
        // Root -> 3 L1 states, each L1 -> 2 L2 states (shared)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["level"] = 0;
        initialState.Variables["value"] = 0;
        var rules = new IRule[]
        {
            LevelTransition(0, 1, 10),
            LevelTransition(0, 1, 20),
            LevelTransition(0, 1, 30),
            LevelTransition(1, 2, 100),
            LevelTransition(1, 2, 200),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(6, result.States.Count);
        Assert.Equal(9, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    #endregion

    #region Reconnecting — Simple Diamond

    [Fact]
    public void Diamond_Classic2Branch_4States4Transitions()
    {
        // S0 -> S1, S0 -> S2, S1 -> S3, S2 -> S3
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10),
            TransitionAt(0, 20),
            TransitionAt(10, 100),
            TransitionAt(20, 100),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertDiamondShape(result, 2, 4, 4);

        // Root outDegree == 2
        Assert.Equal(2, result.Transitions.Count(t => t.SourceStateId == result.StartingStateId));

        // Convergence point has inDegree == 2
        var convergence = result.States.Keys
            .Single(id => result.Transitions.Count(t => t.TargetStateId == id) == 2);
        Assert.Equal(0, result.Transitions.Count(t => t.SourceStateId == convergence));
    }

    [Fact]
    public void Diamond_WithChainPrefix_5States5Transitions()
    {
        // Chain: S0 -> S1, then diamond: S1 -> S2, S1 -> S3, S2 -> S4, S3 -> S4
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 1),
            TransitionAt(1, 10),
            TransitionAt(1, 20),
            TransitionAt(10, 100),
            TransitionAt(20, 100),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertDiamondShape(result, 2, 5, 5);
    }

    [Fact]
    public void Diamond_WithChainSuffix_5States5Transitions()
    {
        // Diamond: S0 -> S1, S0 -> S2, S1 -> S3, S2 -> S3, then chain: S3 -> S4
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10),
            TransitionAt(0, 20),
            TransitionAt(10, 100),
            TransitionAt(20, 100),
            TransitionAt(100, 200),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertDiamondShape(result, 2, 5, 5);
    }

    [Fact]
    public void Diamond_Deep2StepBranches_6States6Transitions()
    {
        // S0 -> S1 -> S3 -> S5, S0 -> S2 -> S4 -> S5
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10),
            TransitionAt(0, 20),
            TransitionAt(10, 11),
            TransitionAt(20, 21),
            TransitionAt(11, 100),
            TransitionAt(21, 100),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertDiamondShape(result, 2, 6, 6);
    }

    #endregion

    #region Reconnecting — Wide Convergence

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Diamond_WideConvergence_NBranchesToSameDescendant(int branchCount)
    {
        // Root -> N children (values 10,20,...), all children -> convergence (value 100)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new List<IRule>();
        for (int i = 0; i < branchCount; i++)
        {
            int childValue = (i + 1) * 10;
            rules.Add(TransitionAt(0, childValue));
            rules.Add(TransitionAt(childValue, 100));
        }

        StateMachine result = builder.Build(initialState, rules.ToArray(), new BuilderConfig());

        int expectedStates = branchCount + 2; // root + N children + 1 convergence
        int expectedTransitions = branchCount * 2; // N root->child + N child->convergence
        AssertDiamondShape(result, branchCount, expectedStates, expectedTransitions);

        // Convergence point has inDegree == branchCount
        var convergence = result.States.Keys
            .Single(id => result.Transitions.Count(t => t.TargetStateId == id) == branchCount);
        Assert.Equal(0, result.Transitions.Count(t => t.SourceStateId == convergence));
    }

    #endregion

    #region Reconnecting — Stacked Diamonds

    [Fact]
    public void StackedDiamond_Two_7States8Transitions()
    {
        // Diamond 1: S0->S1, S0->S2, S1->S3, S2->S3
        // Diamond 2: S3->S4, S3->S5, S4->S6, S5->S6
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10), TransitionAt(0, 20),
            TransitionAt(10, 100), TransitionAt(20, 100),
            TransitionAt(100, 110), TransitionAt(100, 120),
            TransitionAt(110, 200), TransitionAt(120, 200),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(7, result.States.Count);
        Assert.Equal(8, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());

        // Two convergence points (inDegree == 2)
        var convergencePoints = result.States.Keys
            .Where(id => result.Transitions.Count(t => t.TargetStateId == id) == 2)
            .ToList();
        Assert.Equal(2, convergencePoints.Count);
    }

    [Fact]
    public void StackedDiamond_Three_10States12Transitions()
    {
        // Three sequential diamonds
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            // Diamond 1
            TransitionAt(0, 10), TransitionAt(0, 20),
            TransitionAt(10, 100), TransitionAt(20, 100),
            // Diamond 2
            TransitionAt(100, 110), TransitionAt(100, 120),
            TransitionAt(110, 200), TransitionAt(120, 200),
            // Diamond 3
            TransitionAt(200, 210), TransitionAt(200, 220),
            TransitionAt(210, 300), TransitionAt(220, 300),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(10, result.States.Count);
        Assert.Equal(12, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());

        // Three convergence points
        var convergencePoints = result.States.Keys
            .Where(id => result.Transitions.Count(t => t.TargetStateId == id) == 2)
            .ToList();
        Assert.Equal(3, convergencePoints.Count);
    }

    [Fact]
    public void StackedDiamond_MixedBranchCounts_2WayThen3Way()
    {
        // Diamond 1 (2-way): S0->S1, S0->S2, S1->S3, S2->S3
        // Diamond 2 (3-way): S3->S4, S3->S5, S3->S6, S4->S7, S5->S7, S6->S7
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            // Diamond 1 (2-way)
            TransitionAt(0, 10), TransitionAt(0, 20),
            TransitionAt(10, 100), TransitionAt(20, 100),
            // Diamond 2 (3-way)
            TransitionAt(100, 110), TransitionAt(100, 120), TransitionAt(100, 130),
            TransitionAt(110, 200), TransitionAt(120, 200), TransitionAt(130, 200),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(8, result.States.Count);
        Assert.Equal(10, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    #endregion

    #region Reconnecting — Nested Diamonds

    [Fact]
    public void NestedDiamond_OneBranchIsSubDiamond()
    {
        // S0 -> S1 (branch A), S0 -> S2 (branch B)
        // Branch A sub-diamond: S1 -> S3, S1 -> S4, S3 -> S5, S4 -> S5
        // Branch B direct: S2 -> S5
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10), TransitionAt(0, 20),
            TransitionAt(10, 30), TransitionAt(10, 40),
            TransitionAt(30, 100), TransitionAt(40, 100),
            TransitionAt(20, 100),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(6, result.States.Count);
        Assert.Equal(7, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());

        // Convergence point has inDegree == 3 (from S3, S4, and S2)
        var convergence = result.States.Keys
            .Single(id => result.Transitions.Count(t => t.TargetStateId == id) == 3);
        Assert.NotNull(convergence);
    }

    [Fact]
    public void NestedDiamond_BothBranchesContainSubDiamonds()
    {
        // S0 -> S1, S0 -> S2
        // Branch A: S1 -> S3, S1 -> S4, S3 -> S7, S4 -> S7
        // Branch B: S2 -> S5, S2 -> S6, S5 -> S7, S6 -> S7
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10), TransitionAt(0, 20),
            TransitionAt(10, 30), TransitionAt(10, 40),
            TransitionAt(20, 50), TransitionAt(20, 60),
            TransitionAt(30, 100), TransitionAt(40, 100),
            TransitionAt(50, 100), TransitionAt(60, 100),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(8, result.States.Count);
        Assert.Equal(10, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());

        // Convergence point has inDegree == 4
        var convergence = result.States.Keys
            .Single(id => result.Transitions.Count(t => t.TargetStateId == id) == 4);
        Assert.NotNull(convergence);
    }

    [Fact]
    public void NestedDiamond_SubDiamondWithChainThenConvergence()
    {
        // S0 -> S1, S0 -> S2
        // Branch A: S1 -> S3 -> S5 (chain then convergence)
        // Branch B: S2 -> S4 -> S5 (chain then convergence)
        // This is the deep diamond — already tested, but add a chain after convergence
        // S5 -> S6 -> S7
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10), TransitionAt(0, 20),
            TransitionAt(10, 11), TransitionAt(20, 21),
            TransitionAt(11, 100), TransitionAt(21, 100),
            TransitionAt(100, 200), TransitionAt(200, 300),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(8, result.States.Count);
        Assert.Equal(8, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    #endregion

    #region Reconnecting — Fully Connected Graphs

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void FullyConnectedGraph_KNodes_AllToAll(int nodeCount)
    {
        // Use K-1 rules with modular offsets: offset 1..K-1 mod K
        // Each rule produces a distinct successor, and since all K states exist,
        // every state connects to every other state.
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = Enumerable.Range(1, nodeCount - 1)
            .Select(offset => (IRule)ModularOffsetRule(offset, nodeCount))
            .ToArray();

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        AssertFullyConnectedGraph(result, nodeCount);
    }

    #endregion

    #region Hybrid — Chain + Cycle

    [Fact]
    public void Hybrid_ChainBranchesToTerminalAndCycle()
    {
        // Chain: 0->1, branch from 1: terminal path (1->100) and cycle (1->10->11->10)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 1),
            TransitionAt(1, 100),
            TransitionAt(1, 10),
            CycleInRange(10, 2),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(5, result.States.Count);   // S0, S1, S100, S10, S11
        Assert.Equal(5, result.Transitions.Count); // 0->1, 1->100, 1->10, 10->11, 11->10
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());

        // Terminal state (step=100) has outDegree == 0
        var terminalStates = result.States.Keys
            .Where(id => result.Transitions.All(t => t.SourceStateId != id))
            .ToList();
        Assert.Single(terminalStates);
    }

    [Fact]
    public void Hybrid_TwoChainThenCycleSegmentsFromRoot()
    {
        // Root branches to two independent chain-then-cycle segments
        // Branch A: 0->10->11->12->11 (chain 1, cycle 2)
        // Branch B: 0->20->21->22->23->21 (chain 1, cycle 3)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10),
            TransitionAt(0, 20),
            // Branch A: chain to 10, then cycle 11->12->11
            TransitionAt(10, 11),
            CycleInRange(11, 2),
            // Branch B: chain to 20, then cycle 21->22->23->21
            TransitionAt(20, 21),
            CycleInRange(21, 3),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // States: S0, S10, S11, S12, S20, S21, S22, S23 = 8
        Assert.Equal(8, result.States.Count);
        // Transitions: 0->10, 0->20, 10->11, 11->12, 12->11, 20->21, 21->22, 22->23, 23->21 = 9
        Assert.Equal(9, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Hybrid_ChainToSelfLoop()
    {
        // Chain: 0->1->2, then self-loop at 2 (via clone-like behavior producing same state)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 1),
            TransitionAt(1, 2),
            CycleInRange(2, 1), // cycle of length 1 = self-loop
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        Assert.Equal(3, result.States.Count);
        Assert.Equal(3, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    #endregion

    #region Hybrid — Branch + Cycle

    [Fact]
    public void Hybrid_RootBranchesToTerminalChainAndCycle()
    {
        // Root -> chain (100->101->102) and cycle (10->11->12->10)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 100),
            TransitionAt(100, 101),
            TransitionAt(101, 102),
            TransitionAt(0, 10),
            CycleInRange(10, 3),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // States: S0, S100, S101, S102, S10, S11, S12 = 7
        Assert.Equal(7, result.States.Count);
        // Transitions: 0->100, 100->101, 101->102, 0->10, 10->11, 11->12, 12->10 = 7
        Assert.Equal(7, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Hybrid_RootBranchesToTwoIndependentCycles()
    {
        // Root -> cycle A (10->11->10, length 2) and cycle B (20->21->22->20, length 3)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10),
            TransitionAt(0, 20),
            CycleInRange(10, 2),
            CycleInRange(20, 3),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // States: S0, S10, S11, S20, S21, S22 = 6
        Assert.Equal(6, result.States.Count);
        // Transitions: 0->10, 0->20, 10->11, 11->10, 20->21, 21->22, 22->20 = 7
        Assert.Equal(7, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Hybrid_DiamondConvergenceThenCycle()
    {
        // Diamond: 0->10, 0->20, 10->100, 20->100
        // Convergence point 100 enters cycle: 100->101->102->100
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10),
            TransitionAt(0, 20),
            TransitionAt(10, 100),
            TransitionAt(20, 100),
            CycleInRange(100, 3),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // States: S0, S10, S20, S100, S101, S102 = 6
        Assert.Equal(6, result.States.Count);
        // Transitions: 0->10, 0->20, 10->100, 20->100, 100->101, 101->102, 102->100 = 7
        Assert.Equal(7, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());

        // State 100 has inDegree >= 2 (from diamond) + back-edge = 3
        var convergenceId = result.States.Keys
            .Single(id => result.Transitions.Count(t => t.TargetStateId == id) == 3);
        Assert.NotNull(convergenceId);
    }

    [Fact]
    public void Hybrid_TreeWithCycleLeaves()
    {
        // Binary tree depth 1: root -> left (10), root -> right (20)
        // Left enters cycle: 10->11->10
        // Right enters cycle: 20->21->22->20
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10),
            TransitionAt(0, 20),
            CycleInRange(10, 2),
            CycleInRange(20, 3),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // States: S0, S10, S11, S20, S21, S22 = 6
        Assert.Equal(6, result.States.Count);
        // Transitions: 0->10, 0->20, 10->11, 11->10, 20->21, 21->22, 22->20 = 7
        Assert.Equal(7, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    #endregion

    #region Hybrid — Multiple Shape Neighborhoods

    [Fact]
    public void Hybrid_ChainToBranchToCycle_ThreePhase()
    {
        // Phase 1 (chain): 0->1->2
        // Phase 2 (branch): 2->10, 2->20
        // Phase 3 (cycles): 10->11->10, 20->21->22->20
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 1),
            TransitionAt(1, 2),
            TransitionAt(2, 10),
            TransitionAt(2, 20),
            CycleInRange(10, 2),
            CycleInRange(20, 3),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // States: S0, S1, S2, S10, S11, S20, S21, S22 = 8
        Assert.Equal(8, result.States.Count);
        // Transitions: 0->1, 1->2, 2->10, 2->20, 10->11, 11->10, 20->21, 21->22, 22->20 = 9
        Assert.Equal(9, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Hybrid_DiamondWithCyclicBranchAndChainBranch()
    {
        // Diamond-like: 0->10 (chain branch), 0->20 (cyclic branch)
        // Chain branch: 10->100->101 (terminal)
        // Cyclic branch: 20->30->31->30 (enters cycle)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 10),
            TransitionAt(0, 20),
            TransitionAt(10, 100),
            TransitionAt(100, 101),
            TransitionAt(20, 30),
            CycleInRange(30, 2),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // States: S0, S10, S100, S101, S20, S30, S31 = 7
        Assert.Equal(7, result.States.Count);
        // Transitions: 0->10, 0->20, 10->100, 100->101, 20->30, 30->31, 31->30 = 7
        Assert.Equal(7, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Hybrid_FullyConnectedSubgraphFromChain()
    {
        // Chain: 0->1->2, then 2 enters a fully connected 3-node subgraph
        // FC subgraph: nodes 10, 11, 12 with all 6 transitions
        // Entry: 2->10 (step=2 transitions to step=10)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 1),
            TransitionAt(1, 2),
            TransitionAt(2, 10),
            // Fully connected among 10, 11, 12 using modular offsets (mod 3, base 10)
            new FuncRule(
                s => s.Variables.ContainsKey("step") && (int)s.Variables["step"]! >= 10 && (int)s.Variables["step"]! <= 12,
                s => { var c = s.Clone(); c.Variables["step"] = 10 + ((int)c.Variables["step"]! - 10 + 1) % 3; return c; }),
            new FuncRule(
                s => s.Variables.ContainsKey("step") && (int)s.Variables["step"]! >= 10 && (int)s.Variables["step"]! <= 12,
                s => { var c = s.Clone(); c.Variables["step"] = 10 + ((int)c.Variables["step"]! - 10 + 2) % 3; return c; }),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // States: S0, S1, S2, S10, S11, S12 = 6
        Assert.Equal(6, result.States.Count);
        // Transitions: 0->1, 1->2, 2->10, plus 6 FC transitions = 9
        Assert.Equal(9, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Hybrid_TwoDiamondsConnectedByChainWithCycleAtEnd()
    {
        // Diamond 1: 0->10, 0->20, 10->100, 20->100
        // Chain: 100->200
        // Diamond 2: 200->210, 200->220, 210->300, 220->300
        // Cycle at end: 300->301->302->300
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            // Diamond 1
            TransitionAt(0, 10), TransitionAt(0, 20),
            TransitionAt(10, 100), TransitionAt(20, 100),
            // Chain
            TransitionAt(100, 200),
            // Diamond 2
            TransitionAt(200, 210), TransitionAt(200, 220),
            TransitionAt(210, 300), TransitionAt(220, 300),
            // Cycle
            CycleInRange(300, 3),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // States: S0, S10, S20, S100, S200, S210, S220, S300, S301, S302 = 10
        Assert.Equal(10, result.States.Count);
        // Transitions: 4 (diamond1) + 1 (chain) + 4 (diamond2) + 3 (cycle) = 12
        Assert.Equal(12, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    #endregion

    #region Hybrid — Complex Compositions

    [Fact]
    public void Hybrid_BranchWithDifferentTopologyPerArm()
    {
        // Root branches to 3 arms:
        // Arm A: chain (10->11->12, terminal)
        // Arm B: cycle (20->21->20)
        // Arm C: diamond (30->40, 30->50, 40->60, 50->60)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            // Three branches from root
            TransitionAt(0, 10), TransitionAt(0, 20), TransitionAt(0, 30),
            // Arm A: chain
            TransitionAt(10, 11), TransitionAt(11, 12),
            // Arm B: cycle
            CycleInRange(20, 2),
            // Arm C: diamond
            TransitionAt(30, 40), TransitionAt(30, 50),
            TransitionAt(40, 60), TransitionAt(50, 60),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // States: S0, S10, S11, S12, S20, S21, S30, S40, S50, S60 = 10
        Assert.Equal(10, result.States.Count);
        // Transitions: 3 (root) + 2 (chain) + 2 (cycle) + 4 (diamond) = 11
        Assert.Equal(11, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());

        // Verify terminal exists (arm A endpoint)
        var terminalStates = result.States.Keys
            .Where(id => result.Transitions.All(t => t.SourceStateId != id))
            .ToList();
        Assert.Equal(2, terminalStates.Count); // S12 and S60
    }

    [Fact]
    public void Hybrid_ChainToDiamondToCycleThenTerminal()
    {
        // Chain: 0->1
        // Diamond: 1->10, 1->20, 10->100, 20->100
        // Cycle: 100->101->102->100
        // Exit from cycle: 101->200 (terminal)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            TransitionAt(0, 1),
            TransitionAt(1, 10), TransitionAt(1, 20),
            TransitionAt(10, 100), TransitionAt(20, 100),
            CycleInRange(100, 3),
            TransitionAt(101, 200),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // States: S0, S1, S10, S20, S100, S101, S102, S200 = 8
        Assert.Equal(8, result.States.Count);
        // Transitions: 1 (chain) + 4 (diamond) + 3 (cycle) + 1 (exit) = 9
        Assert.Equal(9, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Hybrid_OuterCycleWithInnerBranchContainingSubCycle()
    {
        // Outer cycle: 0->1->2->0
        // Branch from 1: 1->10
        // From 10: branch to chain (10->100, terminal) and sub-cycle (10->20->21->20)
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            CycleInRange(0, 3),
            TransitionAt(1, 10),
            TransitionAt(10, 100),
            TransitionAt(10, 20),
            CycleInRange(20, 2),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // States: S0, S1, S2, S10, S100, S20, S21 = 7
        Assert.Equal(7, result.States.Count);
        // Transitions: 3 (outer cycle) + 1 (branch to 10) + 1 (10->100) + 1 (10->20) + 2 (inner cycle) = 8
        Assert.Equal(8, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());
    }

    [Fact]
    public void Hybrid_CycleWithDiamondExit()
    {
        // Cycle: 0->1->2->0
        // Exit from 2: diamond shape 2->30, 2->40, 30->50, 40->50
        var builder = new StateMachineBuilder();
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var rules = new IRule[]
        {
            CycleInRange(0, 3),
            TransitionAt(2, 30),
            TransitionAt(2, 40),
            TransitionAt(30, 50),
            TransitionAt(40, 50),
        };

        StateMachine result = builder.Build(initialState, rules, new BuilderConfig());

        // States: S0, S1, S2, S30, S40, S50 = 6
        Assert.Equal(6, result.States.Count);
        // Transitions: 3 (cycle) + 4 (diamond from exit) = 7
        Assert.Equal(7, result.Transitions.Count);
        AssertAllStatesReachable(result);
        Assert.True(result.IsValidMachine());

        // Convergence point (step=50) has inDegree == 2
        var convergence = result.States.Keys
            .Single(id => result.Transitions.Count(t => t.TargetStateId == id) == 2);
        Assert.NotNull(convergence);
    }

    #endregion
}