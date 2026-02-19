namespace StateMaker;

public interface IStateMachineImporter
{
    StateMachine Import(string content);
}
