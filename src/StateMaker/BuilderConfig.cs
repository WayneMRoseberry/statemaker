namespace StateMaker;

public enum ExplorationStrategy
{
    BREADTHFIRSTSEARCH,
    DEPTHFIRSTSEARCH
}

public enum LogLevel
{
    INFO,
    DEBUG,
    ERROR
}

public class BuilderConfig
{
    public int? MaxDepth { get; set; }
    public int? MaxStates { get; set; }
    public ExplorationStrategy ExplorationStrategy { get; set; } = ExplorationStrategy.BREADTHFIRSTSEARCH;
    public LogLevel LogLevel { get; set; } = LogLevel.INFO;
}
