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
}