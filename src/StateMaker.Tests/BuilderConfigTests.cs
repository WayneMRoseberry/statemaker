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

    #region 4.0 — Configuration Validation

    private sealed class IncrementRule : IRule
    {
        public bool IsAvailable(State state) => state.Variables.ContainsKey("step") && (int)state.Variables["step"]! < 5;
        public State Execute(State state)
        {
            var c = state.Clone();
            c.Variables["step"] = (int)c.Variables["step"]! + 1;
            return c;
        }
    }

    private static State CreateTestState()
    {
        var s = new State();
        s.Variables["step"] = 0;
        return s;
    }

    // 4.6.1 — Exhaustive mode (both null) with BFS and DFS
    [Theory]
    [InlineData(ExplorationStrategy.BREADTHFIRSTSEARCH)]
    [InlineData(ExplorationStrategy.DEPTHFIRSTSEARCH)]
    public void Build_ExhaustiveMode_BothNull_NoException(ExplorationStrategy strategy)
    {
        var builder = new StateMachineBuilder();
        var config = new BuilderConfig { ExplorationStrategy = strategy };

        var result = builder.Build(CreateTestState(), new IRule[] { new IncrementRule() }, config);

        Assert.True(result.IsValidMachine());
        Assert.True(result.States.Count > 1);
    }

    // 4.6.2 — State-limited mode (MaxStates set, MaxDepth null) with BFS and DFS
    [Theory]
    [InlineData(ExplorationStrategy.BREADTHFIRSTSEARCH)]
    [InlineData(ExplorationStrategy.DEPTHFIRSTSEARCH)]
    public void Build_StateLimitedMode_MaxStatesOnly_NoException(ExplorationStrategy strategy)
    {
        var builder = new StateMachineBuilder();
        var config = new BuilderConfig { MaxStates = 3, ExplorationStrategy = strategy };

        var result = builder.Build(CreateTestState(), new IRule[] { new IncrementRule() }, config);

        Assert.True(result.IsValidMachine());
        Assert.True(result.States.Count <= 3);
    }

    // 4.6.3 — Depth-limited mode (MaxDepth set, MaxStates null) with BFS and DFS
    [Theory]
    [InlineData(ExplorationStrategy.BREADTHFIRSTSEARCH)]
    [InlineData(ExplorationStrategy.DEPTHFIRSTSEARCH)]
    public void Build_DepthLimitedMode_MaxDepthOnly_NoException(ExplorationStrategy strategy)
    {
        var builder = new StateMachineBuilder();
        var config = new BuilderConfig { MaxDepth = 2, ExplorationStrategy = strategy };

        var result = builder.Build(CreateTestState(), new IRule[] { new IncrementRule() }, config);

        Assert.True(result.IsValidMachine());
        Assert.Equal(3, result.States.Count); // S0(step=0), S1(step=1), S2(step=2)
    }

    // 4.6.4 — Dual-limited mode (both set) with BFS and DFS
    [Theory]
    [InlineData(ExplorationStrategy.BREADTHFIRSTSEARCH)]
    [InlineData(ExplorationStrategy.DEPTHFIRSTSEARCH)]
    public void Build_DualLimitedMode_BothSet_NoException(ExplorationStrategy strategy)
    {
        var builder = new StateMachineBuilder();
        var config = new BuilderConfig { MaxStates = 10, MaxDepth = 3, ExplorationStrategy = strategy };

        var result = builder.Build(CreateTestState(), new IRule[] { new IncrementRule() }, config);

        Assert.True(result.IsValidMachine());
        Assert.True(result.States.Count <= 10);
    }

    // 4.6.5 — Valid configs with empty rules and active rules
    [Theory]
    [InlineData(null, null)]
    [InlineData(5, null)]
    [InlineData(null, 3)]
    [InlineData(5, 3)]
    public void Build_ValidConfig_EmptyRules_NoException(int? maxStates, int? maxDepth)
    {
        var builder = new StateMachineBuilder();
        var config = new BuilderConfig { MaxStates = maxStates, MaxDepth = maxDepth };

        var result = builder.Build(CreateTestState(), Array.Empty<IRule>(), config);

        Assert.True(result.IsValidMachine());
        Assert.Single(result.States);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(5, null)]
    [InlineData(null, 3)]
    [InlineData(5, 3)]
    public void Build_ValidConfig_ActiveRules_NoException(int? maxStates, int? maxDepth)
    {
        var builder = new StateMachineBuilder();
        var config = new BuilderConfig { MaxStates = maxStates, MaxDepth = maxDepth };

        var result = builder.Build(CreateTestState(), new IRule[] { new IncrementRule() }, config);

        Assert.True(result.IsValidMachine());
        Assert.True(result.States.Count >= 1);
    }

    // 4.7 — Invalid configuration tests (verifying existing coverage)
    // Null argument tests already exist in StateMachineBuilderTests.cs:
    // - Build_nullInitialState_ThrowsArgumentNullException (4.7.1)
    // - Build_nullRulesArray_ThrowsArgumentNullException (4.7.2)
    // - Build_nullBuildConfig_ThrowsArgumentNullException (4.7.3)
    // - Build_RulesArrayWithNullRuleInIt (4.7.4)
    //
    // The following tests verify these same behaviors from the config validation perspective.

    [Fact]
    public void Build_NullInitialState_ThrowsArgumentNullException()
    {
        var builder = new StateMachineBuilder();
        var config = new BuilderConfig();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            builder.Build(null!, Array.Empty<IRule>(), config));
        Assert.Equal("initialState", ex.ParamName);
    }

    [Fact]
    public void Build_NullRules_ThrowsArgumentNullException()
    {
        var builder = new StateMachineBuilder();
        var config = new BuilderConfig();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            builder.Build(CreateTestState(), null!, config));
        Assert.Equal("rules", ex.ParamName);
    }

    [Fact]
    public void Build_NullConfig_ThrowsArgumentNullException()
    {
        var builder = new StateMachineBuilder();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            builder.Build(CreateTestState(), Array.Empty<IRule>(), null!));
        Assert.Equal("config", ex.ParamName);
    }

    [Fact]
    public void Build_NullRuleElement_ThrowsArgumentNullException()
    {
        var builder = new StateMachineBuilder();
        var config = new BuilderConfig();
        var rules = new IRule[] { new IncrementRule(), null! };

        var ex = Assert.Throws<ArgumentNullException>(() =>
            builder.Build(CreateTestState(), rules, config));
        Assert.Equal("rules[1]", ex.ParamName);
    }

    #endregion
}
