namespace StateMaker;

public class ConsoleLogger : IStateMachineLogger
{
    private readonly LogLevel _logLevel;

    public ConsoleLogger(LogLevel logLevel = LogLevel.INFO)
    {
        _logLevel = logLevel;
    }

    public void LogInfo(string message)
    {
        if (_logLevel == LogLevel.INFO || _logLevel == LogLevel.DEBUG)
            Console.WriteLine($"[INFO] {message}");
    }

    public void LogDebug(string message)
    {
        if (_logLevel == LogLevel.DEBUG)
            Console.WriteLine($"[DEBUG] {message}");
    }

    public void LogError(string message)
    {
        Console.WriteLine($"[ERROR] {message}");
    }
}
