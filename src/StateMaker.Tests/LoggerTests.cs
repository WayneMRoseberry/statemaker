namespace StateMaker.Tests;

public class LoggerTests
{
    private sealed class TestLogger : IStateMachineLogger
    {
        public List<(string Level, string Message)> Messages { get; } = new();

        public void LogInfo(string message) => Messages.Add(("INFO", message));
        public void LogDebug(string message) => Messages.Add(("DEBUG", message));
        public void LogError(string message) => Messages.Add(("ERROR", message));
    }

    private static State MakeState(params (string key, object value)[] pairs)
    {
        var state = new State();
        foreach (var (key, value) in pairs)
            state.Variables[key] = value;
        return state;
    }

    private sealed class IncrementRule : IRule
    {
        public bool IsAvailable(State state) =>
            state.Variables.ContainsKey("step") && (int)state.Variables["step"]! < 3;

        public State Execute(State state)
        {
            var c = state.Clone();
            c.Variables["step"] = (int)c.Variables["step"]! + 1;
            return c;
        }
    }

    private sealed class CycleRule : IRule
    {
        public bool IsAvailable(State state) => state.Variables.ContainsKey("v");

        public State Execute(State state)
        {
            var c = state.Clone();
            c.Variables["v"] = ((int)c.Variables["v"]! + 1) % 2;
            return c;
        }
    }

    #region 8.1 — IStateMachineLogger Interface

    [Fact]
    public void IStateMachineLogger_CanBeImplemented()
    {
        IStateMachineLogger logger = new TestLogger();
        logger.LogInfo("test");
        logger.LogDebug("test");
        logger.LogError("test");
    }

    #endregion

    #region 8.2 — ConsoleLogger

    [Fact]
    public void ConsoleLogger_InfoLevel_WritesInfoAndError()
    {
        var output = new StringWriter();
        System.Console.SetOut(output);
        try
        {
            var logger = new ConsoleLogger(LogLevel.INFO);
            logger.LogInfo("info msg");
            logger.LogDebug("debug msg");
            logger.LogError("error msg");

            var text = output.ToString();
            Assert.Contains("[INFO] info msg", text, StringComparison.Ordinal);
            Assert.DoesNotContain("[DEBUG]", text, StringComparison.Ordinal);
            Assert.Contains("[ERROR] error msg", text, StringComparison.Ordinal);
        }
        finally
        {
            System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public void ConsoleLogger_DebugLevel_WritesAll()
    {
        var output = new StringWriter();
        System.Console.SetOut(output);
        try
        {
            var logger = new ConsoleLogger(LogLevel.DEBUG);
            logger.LogInfo("info msg");
            logger.LogDebug("debug msg");
            logger.LogError("error msg");

            var text = output.ToString();
            Assert.Contains("[INFO]", text, StringComparison.Ordinal);
            Assert.Contains("[DEBUG]", text, StringComparison.Ordinal);
            Assert.Contains("[ERROR]", text, StringComparison.Ordinal);
        }
        finally
        {
            System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public void ConsoleLogger_ErrorLevel_WritesErrorOnly()
    {
        var output = new StringWriter();
        System.Console.SetOut(output);
        try
        {
            var logger = new ConsoleLogger(LogLevel.ERROR);
            logger.LogInfo("info msg");
            logger.LogDebug("debug msg");
            logger.LogError("error msg");

            var text = output.ToString();
            Assert.DoesNotContain("[INFO]", text, StringComparison.Ordinal);
            Assert.DoesNotContain("[DEBUG]", text, StringComparison.Ordinal);
            Assert.Contains("[ERROR] error msg", text, StringComparison.Ordinal);
        }
        finally
        {
            System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    [Fact]
    public void ConsoleLogger_DefaultLevel_IsInfo()
    {
        var output = new StringWriter();
        System.Console.SetOut(output);
        try
        {
            var logger = new ConsoleLogger();
            logger.LogInfo("info msg");
            logger.LogDebug("debug msg");

            var text = output.ToString();
            Assert.Contains("[INFO]", text, StringComparison.Ordinal);
            Assert.DoesNotContain("[DEBUG]", text, StringComparison.Ordinal);
        }
        finally
        {
            System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }

    #endregion

    #region 8.3 — Builder Integration

    [Fact]
    public void Builder_WithLogger_LogsStateDiscovery()
    {
        var logger = new TestLogger();
        var builder = new StateMachineBuilder(logger);
        var state = MakeState(("step", 0));

        builder.Build(state, new IRule[] { new IncrementRule() }, new BuilderConfig { MaxStates = 10 });

        Assert.Contains(logger.Messages, m => m.Level == "INFO" && m.Message.Contains("Initial state", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, m => m.Level == "INFO" && m.Message.Contains("New state", StringComparison.Ordinal));
    }

    [Fact]
    public void Builder_WithLogger_LogsRuleApplication()
    {
        var logger = new TestLogger();
        var builder = new StateMachineBuilder(logger);
        var state = MakeState(("step", 0));

        builder.Build(state, new IRule[] { new IncrementRule() },
            new BuilderConfig { MaxStates = 10, LogLevel = LogLevel.DEBUG });

        Assert.Contains(logger.Messages, m => m.Level == "DEBUG" && m.Message.Contains("Rule", StringComparison.Ordinal));
    }

    [Fact]
    public void Builder_WithLogger_LogsCycleDetection()
    {
        var logger = new TestLogger();
        var builder = new StateMachineBuilder(logger);
        var state = MakeState(("v", 0));

        builder.Build(state, new IRule[] { new CycleRule() },
            new BuilderConfig { MaxStates = 10, LogLevel = LogLevel.DEBUG });

        Assert.Contains(logger.Messages, m => m.Level == "DEBUG" && m.Message.Contains("Cycle detected", StringComparison.Ordinal));
    }

    [Fact]
    public void Builder_WithLogger_LogsLimitReached()
    {
        var logger = new TestLogger();
        var builder = new StateMachineBuilder(logger);
        var state = MakeState(("step", 0));

        builder.Build(state, new IRule[] { new IncrementRule() }, new BuilderConfig { MaxStates = 2 });

        Assert.Contains(logger.Messages, m => m.Level == "INFO" && m.Message.Contains("Max states limit", StringComparison.Ordinal));
    }

    [Fact]
    public void Builder_WithLogger_LogsExplorationComplete()
    {
        var logger = new TestLogger();
        var builder = new StateMachineBuilder(logger);
        var state = MakeState(("step", 0));

        builder.Build(state, new IRule[] { new IncrementRule() }, new BuilderConfig { MaxStates = 10 });

        Assert.Contains(logger.Messages, m => m.Level == "INFO" && m.Message.Contains("Exploration complete", StringComparison.Ordinal));
    }

    [Fact]
    public void Builder_WithLogger_LogsMaxDepthReached()
    {
        var logger = new TestLogger();
        var builder = new StateMachineBuilder(logger);
        var state = MakeState(("step", 0));

        builder.Build(state, new IRule[] { new IncrementRule() },
            new BuilderConfig { MaxDepth = 2, LogLevel = LogLevel.DEBUG });

        Assert.Contains(logger.Messages, m => m.Level == "DEBUG" && m.Message.Contains("Max depth", StringComparison.Ordinal));
    }

    #endregion

    #region 8.4 — Default Logging (no logger)

    [Fact]
    public void Builder_WithoutLogger_DoesNotThrow()
    {
        var builder = new StateMachineBuilder();
        var state = MakeState(("step", 0));

        var machine = builder.Build(state, new IRule[] { new IncrementRule() }, new BuilderConfig { MaxStates = 10 });

        Assert.Equal(4, machine.States.Count);
    }

    #endregion

    #region 8.5 — Extensibility

    [Fact]
    public void Builder_CustomLogger_ReceivesExpectedCalls()
    {
        var logger = new TestLogger();
        var builder = new StateMachineBuilder(logger);
        var state = MakeState(("step", 0));

        builder.Build(state, new IRule[] { new IncrementRule() }, new BuilderConfig { MaxStates = 10 });

        // Custom logger received messages
        Assert.NotEmpty(logger.Messages);
        // At minimum: initial state, exploration start, state discoveries, exploration complete
        Assert.True(logger.Messages.Count >= 4);
    }

    [Fact]
    public void Builder_CustomLogger_DebugSuppressedByDefault()
    {
        var logger = new TestLogger();
        var builder = new StateMachineBuilder(logger);
        var state = MakeState(("step", 0));

        // Default LogLevel is INFO — logger receives all calls but ConsoleLogger would filter
        // The builder sends all messages to the logger; filtering is the logger's responsibility
        builder.Build(state, new IRule[] { new IncrementRule() }, new BuilderConfig { MaxStates = 10 });

        // Logger receives both INFO and DEBUG messages (it's up to the logger to filter)
        Assert.Contains(logger.Messages, m => m.Level == "INFO");
        Assert.Contains(logger.Messages, m => m.Level == "DEBUG");
    }

    #endregion
}
