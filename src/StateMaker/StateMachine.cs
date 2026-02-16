namespace StateMaker;

public class StateMachine
{
    private readonly Dictionary<string, State> _states = new();
    private string? _startingStateId;

    public IReadOnlyDictionary<string, State> States => _states;

    public string? StartingStateId
    {
        get => _startingStateId;
        set
        {
            if (value is not null && !_states.ContainsKey(value))
            {
                throw new StateDoesNotExistException(value);
            }
            _startingStateId = value;
        }
    }

    public List<Transition> Transitions { get; } = new();

    public void AddOrUpdateState(string stateId, State state)
    {
        _states[stateId] = state;
    }

    public bool IsValidMachine()
    {
        if (_states.Count == 0)
            return false;

        if (_startingStateId is null)
            return false;

        foreach (var transition in Transitions)
        {
            if (!_states.ContainsKey(transition.SourceStateId) ||
                !_states.ContainsKey(transition.TargetStateId))
                return false;
        }

        return true;
    }

    // Returns true if the state was successfully removed, false if the state did not exist.
    // If the removed state was the starting state, the starting state is set to null.
    // Note: This method does not remove transitions that reference the removed state. It is the caller's responsibility to ensure that any transitions referencing the removed state are also removed or updated as needed.
    // This design choice allows for more flexible management of transitions, as it does not automatically remove transitions that may still be relevant or needed after a state is removed. It also avoids unintended consequences of automatically removing transitions that may be shared across multiple states.
    public bool RemoveState(string stateId)
    {
        var removed = _states.Remove(stateId);
        if (removed && _startingStateId == stateId)
        {
            _startingStateId = null;
        }
        return removed;
    }
}
