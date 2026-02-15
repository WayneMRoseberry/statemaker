using System.Globalization;

namespace StateMaker.Tests;

public class TestBatteryTests
{
    #region 3.22 — Test Case Generator Verification

    [Fact]
    public void GenerateInitialStates_ProducesMultipleStates()
    {
        var states = TestCaseGenerator.GenerateInitialStates().ToList();

        Assert.True(states.Count >= 10, $"Expected at least 10 initial states, got {states.Count.ToString(CultureInfo.InvariantCulture)}");
    }

    [Fact]
    public void GenerateInitialStates_IncludesEmptyState()
    {
        var states = TestCaseGenerator.GenerateInitialStates().ToList();

        Assert.Contains(states, s => s.Variables.Count == 0);
    }

    [Fact]
    public void GenerateInitialStates_IncludesAllDataTypes()
    {
        var states = TestCaseGenerator.GenerateInitialStates().ToList();
        var allValues = states.SelectMany(s => s.Variables.Values).ToList();

        Assert.Contains(allValues, v => v is string);
        Assert.Contains(allValues, v => v is int);
        Assert.Contains(allValues, v => v is bool);
        Assert.Contains(allValues, v => v is double);
    }

    [Fact]
    public void GenerateConfigs_ProducesMultipleConfigs()
    {
        var configs = TestCaseGenerator.GenerateConfigs().ToList();

        Assert.True(configs.Count >= 20, $"Expected at least 20 configs, got {configs.Count.ToString(CultureInfo.InvariantCulture)}");
    }

    [Fact]
    public void GenerateConfigs_IncludesBothStrategies()
    {
        var configs = TestCaseGenerator.GenerateConfigs().ToList();

        Assert.Contains(configs, c => c.ExplorationStrategy == ExplorationStrategy.BREADTHFIRSTSEARCH);
        Assert.Contains(configs, c => c.ExplorationStrategy == ExplorationStrategy.DEPTHFIRSTSEARCH);
    }

    [Fact]
    public void GenerateConfigs_IncludesNullAndBoundaryLimits()
    {
        var configs = TestCaseGenerator.GenerateConfigs().ToList();

        Assert.Contains(configs, c => c.MaxStates is null);
        Assert.Contains(configs, c => c.MaxStates == 0);
        Assert.Contains(configs, c => c.MaxStates == -1);
        Assert.Contains(configs, c => c.MaxStates == 1);
        Assert.Contains(configs, c => c.MaxDepth is null);
        Assert.Contains(configs, c => c.MaxDepth == 0);
        Assert.Contains(configs, c => c.MaxDepth == -1);
        Assert.Contains(configs, c => c.MaxDepth == 1);
    }

    [Fact]
    public void GenerateRuleVariations_ProducesMultipleVariations()
    {
        var rules = TestCaseGenerator.GenerateRuleVariations().ToList();

        Assert.True(rules.Count >= 6, $"Expected at least 6 rule variations, got {rules.Count.ToString(CultureInfo.InvariantCulture)}");
    }

    [Fact]
    public void GenerateRuleVariations_IncludesEmptyRules()
    {
        var rules = TestCaseGenerator.GenerateRuleVariations().ToList();

        Assert.Contains(rules, r => r.Rules.Length == 0);
    }

    [Fact]
    public void GenerateAll_ProducesNonEmptyCollection()
    {
        var definitions = TestCaseGenerator.GenerateAll().ToList();

        Assert.True(definitions.Count > 100, $"Expected over 100 definitions, got {definitions.Count.ToString(CultureInfo.InvariantCulture)}");
    }

    [Fact]
    public void GenerateAll_AllDefinitionsHaveUniqueNames()
    {
        var definitions = TestCaseGenerator.GenerateAll().ToList();
        var names = definitions.Select(d => d.Name).ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void GenerateAll_AllDefinitionsHaveNonNullFields()
    {
        var definitions = TestCaseGenerator.GenerateAll().ToList();

        Assert.All(definitions, d =>
        {
            Assert.NotNull(d.Name);
            Assert.NotNull(d.InitialState);
            Assert.NotNull(d.Rules);
            Assert.NotNull(d.Config);
        });
    }

    #endregion

    #region 3.23 — Test Battery Executor

    [Fact]
    public void Run_SimpleDefinition_ReturnsPassingResult()
    {
        var state = new State();
        state.Variables["x"] = 0;
        var definition = new BuildDefinition(
            "Simple",
            state,
            Array.Empty<IRule>(),
            new BuilderConfig { MaxStates = 10 });

        var result = TestBatteryExecutor.Run(definition);

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal(1, result.StateCount);
        Assert.Equal(0, result.TransitionCount);
    }

    [Fact]
    public void Run_ValidMachine_PassesAllOracleChecks()
    {
        var state = new State();
        state.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestFuncRule("Inc",
                s => (int)s.Variables["counter"]! < 3,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var definition = new BuildDefinition("Chain3", state, rules, new BuilderConfig { MaxStates = 10, MaxDepth = 5 });

        var result = TestBatteryExecutor.Run(definition);

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal(4, result.StateCount);
        Assert.Equal(3, result.TransitionCount);
    }

    [Fact]
    public void Run_MaxStatesRespected_Passes()
    {
        var state = new State();
        state.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestFuncRule("Inc",
                _ => true,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var definition = new BuildDefinition("Limited", state, rules, new BuilderConfig { MaxStates = 5 });

        var result = TestBatteryExecutor.Run(definition);

        Assert.True(result.Passed, result.FailureReason);
        Assert.True(result.StateCount <= 5);
    }

    [Fact]
    public void Run_MaxDepthRespected_Passes()
    {
        var state = new State();
        state.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestFuncRule("Inc",
                _ => true,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var definition = new BuildDefinition("DepthLimited", state, rules, new BuilderConfig { MaxDepth = 3 });

        var result = TestBatteryExecutor.Run(definition);

        Assert.True(result.Passed, result.FailureReason);
        Assert.True(result.StateCount <= 4); // depth 0,1,2,3 = 4 states max
    }

    [Fact]
    public void RunAll_AllGeneratedDefinitions_PassOracleChecks()
    {
        var definitions = TestCaseGenerator.GenerateAll().ToList();
        var results = TestBatteryExecutor.RunAll(definitions, TimeSpan.FromSeconds(5)).ToList();

        var failures = results.Where(r => !r.Passed).ToList();

        Assert.True(failures.Count == 0,
            $"{failures.Count.ToString(CultureInfo.InvariantCulture)} of {results.Count.ToString(CultureInfo.InvariantCulture)} definitions failed:\n" +
            string.Join("\n", failures.Select(f => $"  - {f.DefinitionName}: {f.FailureReason}")));
    }

    public static IEnumerable<object[]> GeneratedDefinitions()
    {
        // Sample a subset for Theory-based granular reporting
        var definitions = TestCaseGenerator.GenerateAll().ToList();
        // Take every Nth definition for manageable Theory test count
        int step = Math.Max(1, definitions.Count / 50);
        for (int i = 0; i < definitions.Count; i += step)
        {
            yield return new object[] { definitions[i] };
        }
    }

    [Theory]
    [MemberData(nameof(GeneratedDefinitions))]
    public void RunSingle_GeneratedDefinition_PassesOracleChecks(BuildDefinition definition)
    {
        var result = TestBatteryExecutor.Run(definition, TimeSpan.FromSeconds(5));

        Assert.True(result.Passed, $"{definition.Name}: {result.FailureReason}");
    }

    #endregion

    private sealed class TestFuncRule : IRule
    {
        private readonly string _name;
        private readonly Func<State, bool> _isAvailable;
        private readonly Func<State, State> _execute;

        public TestFuncRule(string name, Func<State, bool> isAvailable, Func<State, State> execute)
        {
            _name = name;
            _isAvailable = isAvailable;
            _execute = execute;
        }

        public bool IsAvailable(State state) => _isAvailable(state);
        public State Execute(State state) => _execute(state);
        public string GetName() => _name;
    }
}
