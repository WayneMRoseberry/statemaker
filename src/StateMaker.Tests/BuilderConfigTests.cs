namespace StateMaker.Tests;

public class BuilderConfigTests
{
    [Fact]
    public void Defaults_MaxDepthIsNull()
    {
        var config = new BuilderConfig();

        Assert.Null(config.MaxDepth);
    }

    [Fact]
    public void Defaults_MaxStatesIsNull()
    {
        var config = new BuilderConfig();

        Assert.Null(config.MaxStates);
    }

    [Fact]
    public void Defaults_ExplorationStrategyIsBFS()
    {
        var config = new BuilderConfig();

        Assert.Equal(ExplorationStrategy.BREADTHFIRSTSEARCH, config.ExplorationStrategy);
    }

    [Fact]
    public void Defaults_LogLevelIsINFO()
    {
        var config = new BuilderConfig();

        Assert.Equal(LogLevel.INFO, config.LogLevel);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var config = new BuilderConfig
        {
            MaxDepth = 10,
            MaxStates = 100,
            ExplorationStrategy = ExplorationStrategy.DEPTHFIRSTSEARCH,
            LogLevel = LogLevel.DEBUG
        };

        Assert.Equal(10, config.MaxDepth);
        Assert.Equal(100, config.MaxStates);
        Assert.Equal(ExplorationStrategy.DEPTHFIRSTSEARCH, config.ExplorationStrategy);
        Assert.Equal(LogLevel.DEBUG, config.LogLevel);
    }

    [Fact]
    public void LogLevel_CanBeSetToERROR()
    {
        var config = new BuilderConfig { LogLevel = LogLevel.ERROR };

        Assert.Equal(LogLevel.ERROR, config.LogLevel);
    }

    [Fact]
    public void MaxDepth_CanBeSetAndCleared()
    {
        var config = new BuilderConfig { MaxDepth = 5 };
        Assert.Equal(5, config.MaxDepth);

        config.MaxDepth = null;
        Assert.Null(config.MaxDepth);
    }

    [Fact]
    public void MaxStates_CanBeSetAndCleared()
    {
        var config = new BuilderConfig { MaxStates = 200 };
        Assert.Equal(200, config.MaxStates);

        config.MaxStates = null;
        Assert.Null(config.MaxStates);
    }
}
