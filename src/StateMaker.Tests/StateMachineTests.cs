namespace StateMaker.Tests;

public class StateMachineTests
{
    [Fact]
    public void Constructor_CreatesEmptyStatesAndTransitions()
    {
        var machine = new StateMachine();

        Assert.NotNull(machine.States);
        Assert.Empty(machine.States);
        Assert.NotNull(machine.Transitions);
        Assert.Empty(machine.Transitions);
        Assert.Null(machine.StartingStateId);
    }

    [Fact]
    public void AddState_AddsAndRetrievesState()
    {
        var machine = new StateMachine();
        var state = new State();
        state.Variables["x"] = 1;

        machine.AddState("S0", state);

        Assert.Single(machine.States);
        Assert.Equal(1, machine.States["S0"].Variables["x"]);
    }

    [Fact]
    public void Transitions_CanAddTransitions()
    {
        var machine = new StateMachine();
        machine.Transitions.Add(new Transition("S0", "S1", "Rule1"));
        machine.Transitions.Add(new Transition("S1", "S2", "Rule2"));

        Assert.Equal(2, machine.Transitions.Count);
        Assert.Equal("S0", machine.Transitions[0].SourceStateId);
        Assert.Equal("S1", machine.Transitions[1].SourceStateId);
    }

    [Fact]
    public void StartingStateId_CanBeSetToExistingState()
    {
        var machine = new StateMachine();
        machine.AddState("S0", new State());

        machine.StartingStateId = "S0";

        Assert.Equal("S0", machine.StartingStateId);
    }

    [Fact]
    public void StartingStateId_CanBeSetToNull()
    {
        var machine = new StateMachine();
        machine.AddState("S0", new State());
        machine.StartingStateId = "S0";

        machine.StartingStateId = null;

        Assert.Null(machine.StartingStateId);
    }

    [Fact]
    public void StartingStateId_ThrowsForNonExistentState()
    {
        var machine = new StateMachine();

        var ex = Assert.Throws<StateDoesNotExistException>(
            () => machine.StartingStateId = "S99");

        Assert.Contains("S99", ex.Message);
    }

    [Fact]
    public void RemoveState_ClearsStartingStateIdIfSame()
    {
        var machine = new StateMachine();
        machine.AddState("S0", new State());
        machine.AddState("S1", new State());
        machine.StartingStateId = "S0";

        machine.RemoveState("S0");

        Assert.Null(machine.StartingStateId);
        Assert.False(machine.States.ContainsKey("S0"));
        Assert.Single(machine.States);
    }

    [Fact]
    public void RemoveState_PreservesStartingStateIdIfDifferent()
    {
        var machine = new StateMachine();
        machine.AddState("S0", new State());
        machine.AddState("S1", new State());
        machine.StartingStateId = "S0";

        machine.RemoveState("S1");

        Assert.Equal("S0", machine.StartingStateId);
        Assert.Single(machine.States);
    }

    [Fact]
    public void RemoveState_ReturnsTrueWhenStateExists()
    {
        var machine = new StateMachine();
        machine.AddState("S0", new State());

        Assert.True(machine.RemoveState("S0"));
    }

    [Fact]
    public void RemoveState_ReturnsFalseWhenStateDoesNotExist()
    {
        var machine = new StateMachine();

        Assert.False(machine.RemoveState("S99"));
    }

    [Fact]
    public void States_IsReadOnly()
    {
        var machine = new StateMachine();

        Assert.IsAssignableFrom<IReadOnlyDictionary<string, State>>(machine.States);
    }

    [Fact]
    public void IsValidMachine_SingleStateWithStartingStateId_ReturnsTrue()
    {
        var machine = new StateMachine();
        machine.AddState("S0", new State());
        machine.StartingStateId = "S0";

        Assert.True(machine.IsValidMachine());
    }

    [Fact]
    public void IsValidMachine_TwoStatesWithTransition_ReturnsTrue()
    {
        var machine = new StateMachine();
        machine.AddState("S0", new State());
        machine.AddState("S1", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "Rule1"));

        Assert.True(machine.IsValidMachine());
    }

    [Fact]
    public void IsValidMachine_CycleTransitions_ReturnsTrue()
    {
        var machine = new StateMachine();
        machine.AddState("S0", new State());
        machine.AddState("S1", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "Rule1"));
        machine.Transitions.Add(new Transition("S1", "S0", "Rule1"));

        Assert.True(machine.IsValidMachine());
    }

    [Fact]
    public void IsValidMachine_NoStates_ReturnsFalse()
    {
        var machine = new StateMachine();

        Assert.False(machine.IsValidMachine());
    }

    [Fact]
    public void IsValidMachine_NullStartingStateId_ReturnsFalse()
    {
        var machine = new StateMachine();
        machine.AddState("S0", new State());

        Assert.False(machine.IsValidMachine());
    }

    [Fact]
    public void IsValidMachine_TransitionSourceStateDoesNotExist_ReturnsFalse()
    {
        var machine = new StateMachine();
        machine.AddState("S0", new State());
        machine.AddState("S1", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S99", "S1", "Rule1"));

        Assert.False(machine.IsValidMachine());
    }

    [Fact]
    public void IsValidMachine_TransitionTargetStateDoesNotExist_ReturnsFalse()
    {
        var machine = new StateMachine();
        machine.AddState("S0", new State());
        machine.AddState("S1", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S99", "Rule1"));

        Assert.False(machine.IsValidMachine());
    }
}
