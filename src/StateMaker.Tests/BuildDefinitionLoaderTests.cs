using Xunit;

namespace StateMaker.Tests;

public class BuildDefinitionLoaderTests
{
    #region Null and Invalid Input

    [Fact]
    public void LoadFromJson_NullInput_ThrowsArgumentNullException()
    {
        var loader = new BuildDefinitionLoader();

        Assert.Throws<ArgumentNullException>(() => loader.LoadFromJson(null!));
    }

    [Fact]
    public void LoadFromJson_InvalidJson_ThrowsInvalidOperationException()
    {
        var loader = new BuildDefinitionLoader();

        Assert.Throws<JsonParseException>(() => loader.LoadFromJson("not valid json"));
    }

    [Fact]
    public void LoadFromJson_MissingInitialState_ThrowsInvalidOperationException()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{ ""rules"": [] }";

        var ex = Assert.Throws<InvalidOperationException>(() => loader.LoadFromJson(json));
        Assert.Contains("initialState", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromJson_MissingRules_ThrowsInvalidOperationException()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{ ""initialState"": { ""x"": 1 } }";

        var ex = Assert.Throws<InvalidOperationException>(() => loader.LoadFromJson(json));
        Assert.Contains("rules", ex.Message, StringComparison.Ordinal);
    }

    #endregion

    #region InitialState Parsing

    [Fact]
    public void LoadFromJson_InitialStateWithIntVariable_ParsesCorrectly()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{ ""initialState"": { ""x"": 1 }, ""rules"": [] }";

        var result = loader.LoadFromJson(json);

        Assert.Equal(1, result.InitialState.Variables["x"]);
    }

    [Fact]
    public void LoadFromJson_InitialStateWithStringVariable_ParsesCorrectly()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{ ""initialState"": { ""name"": ""hello"" }, ""rules"": [] }";

        var result = loader.LoadFromJson(json);

        Assert.Equal("hello", result.InitialState.Variables["name"]);
    }

    [Fact]
    public void LoadFromJson_InitialStateWithBoolVariable_ParsesCorrectly()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{ ""initialState"": { ""flag"": true }, ""rules"": [] }";

        var result = loader.LoadFromJson(json);

        Assert.Equal(true, result.InitialState.Variables["flag"]);
    }

    [Fact]
    public void LoadFromJson_InitialStateWithMultipleVariables_ParsesAll()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{ ""initialState"": { ""x"": 1, ""y"": 2, ""z"": 3 }, ""rules"": [] }";

        var result = loader.LoadFromJson(json);

        Assert.Equal(3, result.InitialState.Variables.Count);
        Assert.Equal(1, result.InitialState.Variables["x"]);
        Assert.Equal(2, result.InitialState.Variables["y"]);
        Assert.Equal(3, result.InitialState.Variables["z"]);
    }

    #endregion

    #region Rules Parsing

    [Fact]
    public void LoadFromJson_EmptyRulesArray_ReturnsEmptyRules()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{ ""initialState"": { ""x"": 1 }, ""rules"": [] }";

        var result = loader.LoadFromJson(json);

        Assert.Empty(result.Rules);
    }

    [Fact]
    public void LoadFromJson_SingleDeclarativeRule_ParsesCorrectly()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{
            ""initialState"": { ""x"": 0 },
            ""rules"": [
                {
                    ""name"": ""IncrementX"",
                    ""condition"": ""x < 5"",
                    ""transformations"": { ""x"": ""x + 1"" }
                }
            ]
        }";

        var result = loader.LoadFromJson(json);

        Assert.Single(result.Rules);
        Assert.Equal("IncrementX", result.Rules[0].GetName());
    }

    [Fact]
    public void LoadFromJson_MultipleRules_ParsesAll()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{
            ""initialState"": { ""x"": 0 },
            ""rules"": [
                {
                    ""name"": ""RuleA"",
                    ""condition"": ""x < 5"",
                    ""transformations"": { ""x"": ""x + 1"" }
                },
                {
                    ""name"": ""RuleB"",
                    ""condition"": ""x > 0"",
                    ""transformations"": { ""x"": ""x - 1"" }
                }
            ]
        }";

        var result = loader.LoadFromJson(json);

        Assert.Equal(2, result.Rules.Length);
    }

    #endregion

    #region Config Parsing

    [Fact]
    public void LoadFromJson_NoConfig_ReturnsDefaults()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{ ""initialState"": { ""x"": 1 }, ""rules"": [] }";

        var result = loader.LoadFromJson(json);

        Assert.Null(result.Config.MaxStates);
        Assert.Null(result.Config.MaxDepth);
        Assert.Equal(ExplorationStrategy.BREADTHFIRSTSEARCH, result.Config.ExplorationStrategy);
    }

    [Fact]
    public void LoadFromJson_ConfigWithMaxStates_ParsesCorrectly()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{
            ""initialState"": { ""x"": 1 },
            ""rules"": [],
            ""config"": { ""maxStates"": 50 }
        }";

        var result = loader.LoadFromJson(json);

        Assert.Equal(50, result.Config.MaxStates);
    }

    [Fact]
    public void LoadFromJson_ConfigWithMaxDepth_ParsesCorrectly()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{
            ""initialState"": { ""x"": 1 },
            ""rules"": [],
            ""config"": { ""maxDepth"": 10 }
        }";

        var result = loader.LoadFromJson(json);

        Assert.Equal(10, result.Config.MaxDepth);
    }

    [Fact]
    public void LoadFromJson_ConfigWithBreadthFirstSearch_ParsesCorrectly()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{
            ""initialState"": { ""x"": 1 },
            ""rules"": [],
            ""config"": { ""explorationStrategy"": ""BreadthFirstSearch"" }
        }";

        var result = loader.LoadFromJson(json);

        Assert.Equal(ExplorationStrategy.BREADTHFIRSTSEARCH, result.Config.ExplorationStrategy);
    }

    [Fact]
    public void LoadFromJson_ConfigWithDepthFirstSearch_ParsesCorrectly()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{
            ""initialState"": { ""x"": 1 },
            ""rules"": [],
            ""config"": { ""explorationStrategy"": ""DepthFirstSearch"" }
        }";

        var result = loader.LoadFromJson(json);

        Assert.Equal(ExplorationStrategy.DEPTHFIRSTSEARCH, result.Config.ExplorationStrategy);
    }

    [Fact]
    public void LoadFromJson_ConfigWithAllFields_ParsesAll()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{
            ""initialState"": { ""x"": 1 },
            ""rules"": [],
            ""config"": { ""maxStates"": 100, ""maxDepth"": 20, ""explorationStrategy"": ""DepthFirstSearch"" }
        }";

        var result = loader.LoadFromJson(json);

        Assert.Equal(100, result.Config.MaxStates);
        Assert.Equal(20, result.Config.MaxDepth);
        Assert.Equal(ExplorationStrategy.DEPTHFIRSTSEARCH, result.Config.ExplorationStrategy);
    }

    [Fact]
    public void LoadFromJson_ConfigWithInvalidStrategy_ThrowsInvalidOperationException()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{
            ""initialState"": { ""x"": 1 },
            ""rules"": [],
            ""config"": { ""explorationStrategy"": ""RandomSearch"" }
        }";

        var ex = Assert.Throws<InvalidOperationException>(() => loader.LoadFromJson(json));
        Assert.Contains("RandomSearch", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromJson_EmptyConfig_ReturnsDefaults()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{
            ""initialState"": { ""x"": 1 },
            ""rules"": [],
            ""config"": {}
        }";

        var result = loader.LoadFromJson(json);

        Assert.Null(result.Config.MaxStates);
        Assert.Null(result.Config.MaxDepth);
        Assert.Equal(ExplorationStrategy.BREADTHFIRSTSEARCH, result.Config.ExplorationStrategy);
    }

    #endregion

    #region Integration — Full Build Definition

    [Fact]
    public void LoadFromJson_FullDefinition_AllSectionsParsed()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{
            ""initialState"": { ""counter"": 0 },
            ""rules"": [
                {
                    ""name"": ""Increment"",
                    ""condition"": ""counter < 3"",
                    ""transformations"": { ""counter"": ""counter + 1"" }
                }
            ],
            ""config"": { ""maxStates"": 10, ""maxDepth"": 5 }
        }";

        var result = loader.LoadFromJson(json);

        Assert.Equal(0, result.InitialState.Variables["counter"]);
        Assert.Single(result.Rules);
        Assert.Equal("Increment", result.Rules[0].GetName());
        Assert.Equal(10, result.Config.MaxStates);
        Assert.Equal(5, result.Config.MaxDepth);
    }

    [Fact]
    public void LoadFromJson_RulesCanExecuteAgainstInitialState()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{
            ""initialState"": { ""x"": 0 },
            ""rules"": [
                {
                    ""name"": ""Inc"",
                    ""condition"": ""x < 2"",
                    ""transformations"": { ""x"": ""x + 1"" }
                }
            ]
        }";

        var result = loader.LoadFromJson(json);

        Assert.True(result.Rules[0].IsAvailable(result.InitialState));
        var newState = result.Rules[0].Execute(result.InitialState);
        Assert.Equal(1, newState.Variables["x"]);
    }

    [Fact]
    public void LoadFromJson_CanBuildStateMachineFromResult()
    {
        var loader = new BuildDefinitionLoader();
        var json = @"{
            ""initialState"": { ""step"": 0 },
            ""rules"": [
                {
                    ""name"": ""Step"",
                    ""condition"": ""step < 3"",
                    ""transformations"": { ""step"": ""step + 1"" }
                }
            ],
            ""config"": { ""maxStates"": 10 }
        }";

        var result = loader.LoadFromJson(json);
        var builder = new StateMachineBuilder();
        var stateMachine = builder.Build(result.InitialState, result.Rules, result.Config);

        Assert.Equal(4, stateMachine.States.Count);
        Assert.Equal(3, stateMachine.Transitions.Count);
        Assert.True(stateMachine.IsValidMachine());
    }

    #endregion

    #region LoadFromFile

    [Fact]
    public void LoadFromFile_FileNotFound_ThrowsFileNotFoundException()
    {
        var loader = new BuildDefinitionLoader();

        Assert.Throws<FileNotFoundException>(() =>
            loader.LoadFromFile("nonexistent-file.json"));
    }

    #endregion
}
