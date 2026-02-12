namespace StateMaker;

public interface IStateMachineBuilder
{
    StateMachine Build(State initialState, IRule[] rules, BuilderConfig config);
}