namespace StateMaker;

public class StateDoesNotExistException : Exception
{
    public StateDoesNotExistException(string stateId)
        : base($"State '{stateId}' does not exist in the state machine.")
    {
    }
}
