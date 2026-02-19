namespace StateMaker;

public interface IStateMachineLogger
{
    void LogInfo(string message);
    void LogDebug(string message);
    void LogError(string message);
}
