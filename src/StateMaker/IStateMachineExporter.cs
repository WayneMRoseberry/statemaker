namespace StateMaker;

public interface IStateMachineExporter
{
    string Export(StateMachine stateMachine, ExportType exportType = ExportType.Default, bool includeStateVariables = false);
}
