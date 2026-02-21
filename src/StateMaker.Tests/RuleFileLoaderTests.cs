namespace StateMaker.Tests;

public class RuleFileLoaderTests
{
    private readonly RuleFileLoader _loader = new(new ExpressionEvaluator());

    #region 6.6 — Initial State Parsing

    [Fact]
    public void LoadFromJson_WithInitialState_ReturnsState()
    {
        var json = @"{
            ""initialState"": { ""Status"": ""Pending"", ""Count"": 0, ""IsActive"": true, ""Price"": 9.99 },
            ""rules"": []
        }";

        var (initialState, _) = _loader.LoadFromJson(json);

        Assert.NotNull(initialState);
        Assert.Equal("Pending", initialState!.Variables["Status"]);
        Assert.Equal(0, (int)initialState.Variables["Count"]!);
        Assert.Equal(true, initialState.Variables["IsActive"]);
        Assert.Equal(9.99, (double)initialState.Variables["Price"]!);
    }

    [Fact]
    public void LoadFromJson_WithoutInitialState_ReturnsNull()
    {
        var json = @"{ ""rules"": [] }";

        var (initialState, _) = _loader.LoadFromJson(json);

        Assert.Null(initialState);
    }

    [Fact]
    public void LoadFromJson_NullInitialState_ReturnsNull()
    {
        var json = @"{ ""initialState"": null, ""rules"": [] }";

        var (initialState, _) = _loader.LoadFromJson(json);

        Assert.Null(initialState);
    }

    #endregion

    #region 6.7 — Declarative Rule Parsing

    [Fact]
    public void LoadFromJson_SingleDeclarativeRule_CreatesRule()
    {
        var json = @"{
            ""rules"": [
                {
                    ""name"": ""ApproveRule"",
                    ""condition"": ""[Status] == 'Pending'"",
                    ""transformations"": { ""Status"": ""'Approved'"" }
                }
            ]
        }";

        var (_, rules) = _loader.LoadFromJson(json);

        Assert.Single(rules);
        Assert.IsType<DeclarativeRule>(rules[0]);
        Assert.Equal("ApproveRule", rules[0].GetName());
    }

    [Fact]
    public void LoadFromJson_DeclarativeRuleWithExplicitType_CreatesRule()
    {
        var json = @"{
            ""rules"": [
                {
                    ""type"": ""declarative"",
                    ""name"": ""MyRule"",
                    ""condition"": ""true"",
                    ""transformations"": { ""x"": ""1"" }
                }
            ]
        }";

        var (_, rules) = _loader.LoadFromJson(json);

        Assert.Single(rules);
        Assert.IsType<DeclarativeRule>(rules[0]);
        Assert.Equal("MyRule", rules[0].GetName());
    }

    [Fact]
    public void LoadFromJson_MultipleDeclarativeRules_CreatesAll()
    {
        var json = @"{
            ""rules"": [
                {
                    ""name"": ""Rule1"",
                    ""condition"": ""true"",
                    ""transformations"": { ""x"": ""1"" }
                },
                {
                    ""name"": ""Rule2"",
                    ""condition"": ""[x] == 1"",
                    ""transformations"": { ""x"": ""2"" }
                }
            ]
        }";

        var (_, rules) = _loader.LoadFromJson(json);

        Assert.Equal(2, rules.Length);
        Assert.Equal("Rule1", rules[0].GetName());
        Assert.Equal("Rule2", rules[1].GetName());
    }

    [Fact]
    public void LoadFromJson_DeclarativeRuleWithMultipleTransformations()
    {
        var json = @"{
            ""rules"": [
                {
                    ""name"": ""Process"",
                    ""condition"": ""true"",
                    ""transformations"": { ""Status"": ""'Done'"", ""Count"": ""[Count] + 1"" }
                }
            ]
        }";

        var (_, rules) = _loader.LoadFromJson(json);
        var state = new State();
        state.Variables["Status"] = "Pending";
        state.Variables["Count"] = 5;

        var result = rules[0].Execute(state);
        Assert.Equal("Done", result.Variables["Status"]);
        Assert.Equal(6, result.Variables["Count"]);
    }

    [Fact]
    public void LoadFromJson_DeclarativeRuleWithEmptyTransformations()
    {
        var json = @"{
            ""rules"": [
                {
                    ""name"": ""NoOp"",
                    ""condition"": ""true"",
                    ""transformations"": {}
                }
            ]
        }";

        var (_, rules) = _loader.LoadFromJson(json);
        Assert.Single(rules);
    }

    #endregion

    #region 6.7 — Declarative Rule Functional Verification

    [Fact]
    public void LoadFromJson_RuleIsAvailable_EvaluatesCondition()
    {
        var json = @"{
            ""rules"": [
                {
                    ""name"": ""CheckStatus"",
                    ""condition"": ""[Status] == 'Pending'"",
                    ""transformations"": { ""Status"": ""'Approved'"" }
                }
            ]
        }";

        var (_, rules) = _loader.LoadFromJson(json);
        var pendingState = new State();
        pendingState.Variables["Status"] = "Pending";
        var approvedState = new State();
        approvedState.Variables["Status"] = "Approved";

        Assert.True(rules[0].IsAvailable(pendingState));
        Assert.False(rules[0].IsAvailable(approvedState));
    }

    #endregion

    #region 6.8 — Custom Rule Loading

    [Fact]
    public void LoadFromJson_CustomRule_LoadsFromAssembly()
    {
        // Use the test assembly itself which contains IRule implementations
        var assemblyPath = typeof(RuleFileLoaderTests).Assembly.Location;
        var className = typeof(TestCustomRule).FullName;

        var json = $@"{{
            ""rules"": [
                {{
                    ""type"": ""custom"",
                    ""assemblyPath"": ""{assemblyPath.Replace("\\", "\\\\")}"",
                    ""className"": ""{className}""
                }}
            ]
        }}";

        var (_, rules) = _loader.LoadFromJson(json);

        Assert.Single(rules);
        Assert.IsType<TestCustomRule>(rules[0]);
    }

    [Fact]
    public void LoadFromJson_MixedRules_LoadsBothTypes()
    {
        var assemblyPath = typeof(RuleFileLoaderTests).Assembly.Location;
        var className = typeof(TestCustomRule).FullName;

        var json = $@"{{
            ""rules"": [
                {{
                    ""name"": ""DeclRule"",
                    ""condition"": ""true"",
                    ""transformations"": {{ ""x"": ""1"" }}
                }},
                {{
                    ""type"": ""custom"",
                    ""assemblyPath"": ""{assemblyPath.Replace("\\", "\\\\")}"",
                    ""className"": ""{className}""
                }}
            ]
        }}";

        var (_, rules) = _loader.LoadFromJson(json);

        Assert.Equal(2, rules.Length);
        Assert.IsType<DeclarativeRule>(rules[0]);
        Assert.IsType<TestCustomRule>(rules[1]);
    }

    #endregion

    #region 6.9 — Validation and Error Messages

    [Fact]
    public void LoadFromJson_InvalidJson_Throws()
    {
        var ex = Assert.Throws<JsonParseException>(() =>
            _loader.LoadFromJson("not valid json {{{"));
        Assert.Contains("JSON", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromJson_MissingRulesArray_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _loader.LoadFromJson(@"{ ""initialState"": {} }"));
        Assert.Contains("rules", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromJson_DeclarativeRule_MissingName_Throws()
    {
        var json = @"{
            ""rules"": [
                {
                    ""condition"": ""true"",
                    ""transformations"": { ""x"": ""1"" }
                }
            ]
        }";

        var ex = Assert.Throws<InvalidOperationException>(() => _loader.LoadFromJson(json));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromJson_DeclarativeRule_MissingCondition_Throws()
    {
        var json = @"{
            ""rules"": [
                {
                    ""name"": ""Rule1"",
                    ""transformations"": { ""x"": ""1"" }
                }
            ]
        }";

        var ex = Assert.Throws<InvalidOperationException>(() => _loader.LoadFromJson(json));
        Assert.Contains("condition", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromJson_CustomRule_MissingClassName_Throws()
    {
        var json = @"{
            ""rules"": [
                {
                    ""type"": ""custom"",
                    ""assemblyPath"": ""some.dll""
                }
            ]
        }";

        var ex = Assert.Throws<InvalidOperationException>(() => _loader.LoadFromJson(json));
        Assert.Contains("className", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromJson_CustomRule_MissingAssemblyPath_Throws()
    {
        var json = @"{
            ""rules"": [
                {
                    ""type"": ""custom"",
                    ""className"": ""Some.Class""
                }
            ]
        }";

        var ex = Assert.Throws<InvalidOperationException>(() => _loader.LoadFromJson(json));
        Assert.Contains("assemblyPath", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromJson_CustomRule_AssemblyNotFound_Throws()
    {
        var json = @"{
            ""rules"": [
                {
                    ""type"": ""custom"",
                    ""assemblyPath"": ""nonexistent.dll"",
                    ""className"": ""Some.Class""
                }
            ]
        }";

        var ex = Assert.Throws<InvalidOperationException>(() => _loader.LoadFromJson(json));
        Assert.Contains("assembly", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromJson_CustomRule_ClassNotFound_Throws()
    {
        var assemblyPath = typeof(RuleFileLoaderTests).Assembly.Location;

        var json = $@"{{
            ""rules"": [
                {{
                    ""type"": ""custom"",
                    ""assemblyPath"": ""{assemblyPath.Replace("\\", "\\\\")}"",
                    ""className"": ""NonExistent.ClassName""
                }}
            ]
        }}";

        var ex = Assert.Throws<InvalidOperationException>(() => _loader.LoadFromJson(json));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromJson_CustomRule_DoesNotImplementIRule_Throws()
    {
        var assemblyPath = typeof(RuleFileLoaderTests).Assembly.Location;
        var className = typeof(NotARuleClass).FullName;

        var json = $@"{{
            ""rules"": [
                {{
                    ""type"": ""custom"",
                    ""assemblyPath"": ""{assemblyPath.Replace("\\", "\\\\")}"",
                    ""className"": ""{className}""
                }}
            ]
        }}";

        var ex = Assert.Throws<InvalidOperationException>(() => _loader.LoadFromJson(json));
        Assert.Contains("IRule", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromJson_CustomRule_NoParameterlessConstructor_Throws()
    {
        var assemblyPath = typeof(RuleFileLoaderTests).Assembly.Location;
        var className = typeof(NoDefaultConstructorRule).FullName;

        var json = $@"{{
            ""rules"": [
                {{
                    ""type"": ""custom"",
                    ""assemblyPath"": ""{assemblyPath.Replace("\\", "\\\\")}"",
                    ""className"": ""{className}""
                }}
            ]
        }}";

        var ex = Assert.Throws<InvalidOperationException>(() => _loader.LoadFromJson(json));
        Assert.Contains("constructor", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromJson_UnknownRuleType_Throws()
    {
        var json = @"{
            ""rules"": [
                {
                    ""type"": ""unknown"",
                    ""name"": ""Rule1""
                }
            ]
        }";

        var ex = Assert.Throws<InvalidOperationException>(() => _loader.LoadFromJson(json));
        Assert.Contains("unknown", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromJson_NullJson_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _loader.LoadFromJson(null!));
    }

    #endregion

    #region 6.5 — File Loading

    [Fact]
    public void LoadFromFile_ValidFile_LoadsRules()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"{
                ""initialState"": { ""x"": 0 },
                ""rules"": [
                    {
                        ""name"": ""Inc"",
                        ""condition"": ""[x] < 3"",
                        ""transformations"": { ""x"": ""[x] + 1"" }
                    }
                ]
            }");

            var (initialState, rules) = _loader.LoadFromFile(tempFile);

            Assert.NotNull(initialState);
            Assert.Equal(0, (int)initialState!.Variables["x"]!);
            Assert.Single(rules);
            Assert.Equal("Inc", rules[0].GetName());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadFromFile_FileNotFound_Throws()
    {
        var ex = Assert.Throws<FileNotFoundException>(() =>
            _loader.LoadFromFile("/nonexistent/path/rules.json"));
    }

    #endregion

    #region Integration

    [Fact]
    public void LoadFromJson_FullIntegration_ProducesStateMachine()
    {
        var json = @"{
            ""initialState"": { ""step"": 0 },
            ""rules"": [
                {
                    ""name"": ""Increment"",
                    ""condition"": ""[step] < 3"",
                    ""transformations"": { ""step"": ""[step] + 1"" }
                }
            ]
        }";

        var (initialState, rules) = _loader.LoadFromJson(json);
        var builder = new StateMachineBuilder();
        var machine = builder.Build(initialState!, rules, new BuilderConfig { MaxStates = 10 });

        Assert.Equal(4, machine.States.Count);
        Assert.Equal(3, machine.Transitions.Count);
        Assert.True(machine.IsValidMachine());
    }

    #endregion
}

// Test helper classes for custom rule loading tests
public class TestCustomRule : IRule
{
    public bool IsAvailable(State state) => true;
    public State Execute(State state) => state.Clone();
}

public class NotARuleClass
{
    public string Name { get; set; } = "NotARule";
}

public class NoDefaultConstructorRule : IRule
{
    private readonly string _value;

    public NoDefaultConstructorRule(string value)
    {
        _value = value;
    }

    public bool IsAvailable(State state) => true;
    public State Execute(State state) => state.Clone();
}
