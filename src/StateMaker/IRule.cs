namespace StateMaker;

public interface IRule
{
    bool IsAvailable(State state);
    State Execute(State state);
}
