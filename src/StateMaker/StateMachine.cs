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

    public void AddState(string stateId, State state)
    {
        _states[stateId] = state;
    }

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
