namespace StateMaker;

public interface IStateMachineExporter
{
    string Export(StateMachine stateMachine, string exportType = "Default");
}
