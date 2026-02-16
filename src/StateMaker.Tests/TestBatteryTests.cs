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

    #region 3.24 — Performance Oracle and Shape Matching

    [Fact]
    public void Run_ElapsedTimeIsPopulated()
    {
        var state = new State();
        state.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestFuncRule("Inc",
                s => (int)s.Variables["counter"]! < 5,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var definition = new BuildDefinition("TimingTest", state, rules, new BuilderConfig());

        var result = TestBatteryExecutor.Run(definition);

        Assert.True(result.Passed, result.FailureReason);
        Assert.True(result.ElapsedTime > TimeSpan.Zero);
    }

    [Fact]
    public void Run_ShapeMatch_CorrectShape_Passes()
    {
        var state = new State();
        state.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestFuncRule("Inc",
                s => (int)s.Variables["counter"]! < 3,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var expected = new ExpectedShapeInfo(4, 3, 3);
        var definition = new BuildDefinition("ShapeMatch", state, rules, new BuilderConfig(), expected);

        var result = TestBatteryExecutor.Run(definition);

        Assert.True(result.Passed, result.FailureReason);
    }

    [Fact]
    public void Run_ShapeMatch_WrongStateCount_Fails()
    {
        var state = new State();
        state.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestFuncRule("Inc",
                s => (int)s.Variables["counter"]! < 3,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var expected = new ExpectedShapeInfo(10, null, null); // wrong state count
        var definition = new BuildDefinition("WrongShape", state, rules, new BuilderConfig(), expected);

        var result = TestBatteryExecutor.Run(definition);

        Assert.False(result.Passed);
        Assert.Contains("Shape mismatch", result.FailureReason);
        Assert.Contains("states", result.FailureReason);
    }

    [Fact]
    public void Run_ShapeMatch_WrongTransitionCount_Fails()
    {
        var state = new State();
        state.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestFuncRule("Inc",
                s => (int)s.Variables["counter"]! < 3,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var expected = new ExpectedShapeInfo(4, 99, null); // wrong transition count
        var definition = new BuildDefinition("WrongTransitions", state, rules, new BuilderConfig(), expected);

        var result = TestBatteryExecutor.Run(definition);

        Assert.False(result.Passed);
        Assert.Contains("Shape mismatch", result.FailureReason);
        Assert.Contains("transitions", result.FailureReason);
    }

    [Fact]
    public void Run_ShapeMatch_WrongMaxDepth_Fails()
    {
        var state = new State();
        state.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestFuncRule("Inc",
                s => (int)s.Variables["counter"]! < 3,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        var expected = new ExpectedShapeInfo(4, 3, 99); // wrong max depth
        var definition = new BuildDefinition("WrongDepth", state, rules, new BuilderConfig(), expected);

        var result = TestBatteryExecutor.Run(definition);

        Assert.False(result.Passed);
        Assert.Contains("Shape mismatch", result.FailureReason);
        Assert.Contains("depth", result.FailureReason);
    }

    [Fact]
    public void Run_ShapeMatch_PartialExpected_OnlyChecksSpecifiedFields()
    {
        var state = new State();
        state.Variables["counter"] = 0;
        var rules = new IRule[]
        {
            new TestFuncRule("Inc",
                s => (int)s.Variables["counter"]! < 3,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        };
        // Only check state count, leave others null
        var expected = new ExpectedShapeInfo(4, null, null);
        var definition = new BuildDefinition("PartialShape", state, rules, new BuilderConfig(), expected);

        var result = TestBatteryExecutor.Run(definition);

        Assert.True(result.Passed, result.FailureReason);
    }

    #endregion

    #region 3.25 — Reverse Rule Generator

    [Fact]
    public void ReverseGenerator_GenerateAllShapes_ProducesDefinitions()
    {
        var definitions = ReverseRuleGenerator.GenerateAllShapes().ToList();

        Assert.True(definitions.Count >= 30, $"Expected at least 30 reverse-generated definitions, got {definitions.Count.ToString(CultureInfo.InvariantCulture)}");
    }

    [Fact]
    public void ReverseGenerator_AllDefinitionsHaveExpectedShape()
    {
        var definitions = ReverseRuleGenerator.GenerateAllShapes().ToList();

        Assert.All(definitions, d => Assert.NotNull(d.ExpectedShape));
    }

    [Fact]
    public void ReverseGenerator_AllDefinitionsHaveUniqueNames()
    {
        var definitions = ReverseRuleGenerator.GenerateAllShapes().ToList();
        var names = definitions.Select(d => d.Name).ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void RunAll_ReverseGeneratedDefinitions_PassAllOracles()
    {
        var definitions = ReverseRuleGenerator.GenerateAllShapes().ToList();
        var results = TestBatteryExecutor.RunAll(definitions, TimeSpan.FromSeconds(5)).ToList();

        var failures = results.Where(r => !r.Passed).ToList();

        Assert.True(failures.Count == 0,
            $"{failures.Count.ToString(CultureInfo.InvariantCulture)} of {results.Count.ToString(CultureInfo.InvariantCulture)} reverse-generated definitions failed:\n" +
            string.Join("\n", failures.Select(f => $"  - {f.DefinitionName}: {f.FailureReason}")));
    }

    public static IEnumerable<object[]> ReverseGeneratedDefinitions()
    {
        foreach (var def in ReverseRuleGenerator.GenerateAllShapes())
        {
            yield return new object[] { def };
        }
    }

    [Theory]
    [MemberData(nameof(ReverseGeneratedDefinitions))]
    public void RunSingle_ReverseGenerated_PassesAllOracles(BuildDefinition definition)
    {
        var result = TestBatteryExecutor.Run(definition, TimeSpan.FromSeconds(5));

        Assert.True(result.Passed, $"{definition.Name}: {result.FailureReason}");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void ReverseGenerator_Chain_ProducesCorrectShape(int length)
    {
        var definitions = ReverseRuleGenerator.GenerateChain(length).ToList();

        Assert.True(definitions.Count >= 3); // base + non-trigger + reversed
        Assert.All(definitions, d =>
        {
            Assert.Equal(length + 1, d.ExpectedShape!.ExpectedStateCount);
            Assert.Equal(length, d.ExpectedShape.ExpectedTransitionCount);
        });
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void ReverseGenerator_Cycle_ProducesCorrectShape(int length)
    {
        var definitions = ReverseRuleGenerator.GenerateCycle(length).ToList();

        Assert.True(definitions.Count >= 3);
        Assert.All(definitions, d =>
        {
            Assert.Equal(length, d.ExpectedShape!.ExpectedStateCount);
            Assert.Equal(length, d.ExpectedShape.ExpectedTransitionCount);
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ReverseGenerator_BinaryTree_ProducesCorrectShape(int depth)
    {
        var definitions = ReverseRuleGenerator.GenerateBinaryTree(depth).ToList();
        int expectedStates = (1 << (depth + 1)) - 1;

        Assert.True(definitions.Count >= 3);
        Assert.All(definitions, d =>
        {
            Assert.Equal(expectedStates, d.ExpectedShape!.ExpectedStateCount);
            Assert.Equal(expectedStates - 1, d.ExpectedShape.ExpectedTransitionCount);
            Assert.Equal(depth, d.ExpectedShape.ExpectedMaxDepth);
        });
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ReverseGenerator_Diamond_ProducesCorrectShape(int branchCount)
    {
        var definitions = ReverseRuleGenerator.GenerateDiamond(branchCount).ToList();

        Assert.True(definitions.Count >= 3);
        Assert.All(definitions, d =>
        {
            Assert.Equal(branchCount + 2, d.ExpectedShape!.ExpectedStateCount);
            Assert.Equal(branchCount * 2, d.ExpectedShape.ExpectedTransitionCount);
            Assert.Equal(2, d.ExpectedShape.ExpectedMaxDepth);
        });
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ReverseGenerator_FullyConnected_ProducesCorrectShape(int nodeCount)
    {
        var definitions = ReverseRuleGenerator.GenerateFullyConnected(nodeCount).ToList();

        Assert.True(definitions.Count >= 3);
        Assert.All(definitions, d =>
        {
            Assert.Equal(nodeCount, d.ExpectedShape!.ExpectedStateCount);
            Assert.Equal(nodeCount * (nodeCount - 1), d.ExpectedShape.ExpectedTransitionCount);
        });
    }

    #endregion

    #region Rule Ordering Equivalence

    [Fact]
    public void ReverseGenerator_ShuffledVariations_IncludedInOutput()
    {
        // Verify that shapes with multiple rules produce shuffled/rotated variations
        var definitions = ReverseRuleGenerator.GenerateAllShapes().ToList();
        var names = definitions.Select(d => d.Name).ToList();

        Assert.Contains(names, n => n.Contains("Rotated", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void ReverseGenerator_Chain_ShuffledVariations_ProduceSameShape(int length)
    {
        var definitions = ReverseRuleGenerator.GenerateChain(length).ToList();
        var shuffled = definitions.Where(d => d.Name.Contains("Rotated", StringComparison.Ordinal)
            || d.Name.Contains("Reversed", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(shuffled);
        var results = shuffled.Select(d => TestBatteryExecutor.Run(d, TimeSpan.FromSeconds(5))).ToList();
        Assert.All(results, r => Assert.True(r.Passed, $"{r.DefinitionName}: {r.FailureReason}"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void ReverseGenerator_Diamond_ShuffledVariations_ProduceSameShape(int branchCount)
    {
        var definitions = ReverseRuleGenerator.GenerateDiamond(branchCount).ToList();
        var shuffled = definitions.Where(d => d.Name.Contains("Rotated", StringComparison.Ordinal)
            || d.Name.Contains("Reversed", StringComparison.Ordinal)
            || d.Name.Contains("Interleaved", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(shuffled);
        var results = shuffled.Select(d => TestBatteryExecutor.Run(d, TimeSpan.FromSeconds(5))).ToList();
        Assert.All(results, r => Assert.True(r.Passed, $"{r.DefinitionName}: {r.FailureReason}"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void ReverseGenerator_FullyConnected_ShuffledVariations_ProduceSameShape(int nodeCount)
    {
        var definitions = ReverseRuleGenerator.GenerateFullyConnected(nodeCount).ToList();
        var shuffled = definitions.Where(d => d.Name.Contains("Rotated", StringComparison.Ordinal)
            || d.Name.Contains("Reversed", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(shuffled);
        var results = shuffled.Select(d => TestBatteryExecutor.Run(d, TimeSpan.FromSeconds(5))).ToList();
        Assert.All(results, r => Assert.True(r.Passed, $"{r.DefinitionName}: {r.FailureReason}"));
    }

    #endregion

    #region Rule Split/Merge Equivalence

    [Fact]
    public void ReverseGenerator_SplitVariations_IncludedInOutput()
    {
        var definitions = ReverseRuleGenerator.GenerateAllShapes().ToList();
        var names = definitions.Select(d => d.Name).ToList();

        Assert.Contains(names, n => n.Contains("Split", StringComparison.Ordinal));
        Assert.Contains(names, n => n.Contains("SplitReversed", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void ReverseGenerator_Chain_SplitRules_ProduceSameShape(int length)
    {
        var definitions = ReverseRuleGenerator.GenerateChain(length).ToList();
        var splitDefs = definitions.Where(d => d.Name.Contains("Split", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(splitDefs);
        // All split variations should have multiple rules with same GetName
        foreach (var def in splitDefs)
        {
            if (!def.Name.Contains("Shuffled", StringComparison.Ordinal))
            {
                // Split rules should have more rules than the base (1 rule split into N)
                Assert.True(def.Rules.Length > 1,
                    $"{def.Name} should have multiple rules, got {def.Rules.Length.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        var results = splitDefs.Select(d => TestBatteryExecutor.Run(d, TimeSpan.FromSeconds(5))).ToList();
        Assert.All(results, r => Assert.True(r.Passed, $"{r.DefinitionName}: {r.FailureReason}"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void ReverseGenerator_Cycle_SplitRules_ProduceSameShape(int length)
    {
        var definitions = ReverseRuleGenerator.GenerateCycle(length).ToList();
        var splitDefs = definitions.Where(d => d.Name.Contains("Split", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(splitDefs);
        // Split cycle should have 'length' rules, all named "CycleStep"
        var baseSplit = splitDefs.First(d => d.Name.EndsWith("_Split", StringComparison.Ordinal));
        Assert.Equal(length, baseSplit.Rules.Length);
        Assert.All(baseSplit.Rules, r => Assert.Equal("CycleStep", r.GetName()));

        var results = splitDefs.Select(d => TestBatteryExecutor.Run(d, TimeSpan.FromSeconds(5))).ToList();
        Assert.All(results, r => Assert.True(r.Passed, $"{r.DefinitionName}: {r.FailureReason}"));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 3)]
    public void ReverseGenerator_ChainThenCycle_SplitRules_ProduceSameShape(int chainLen, int cycleLen)
    {
        var definitions = ReverseRuleGenerator.GenerateChainThenCycle(chainLen, cycleLen).ToList();
        var splitDefs = definitions.Where(d => d.Name.Contains("Split", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(splitDefs);
        // Split should have chainLen + cycleLen rules, all named "Advance"
        var baseSplit = splitDefs.First(d => d.Name.EndsWith("_Split", StringComparison.Ordinal));
        Assert.Equal(chainLen + cycleLen, baseSplit.Rules.Length);
        Assert.All(baseSplit.Rules, r => Assert.Equal("Advance", r.GetName()));

        var results = splitDefs.Select(d => TestBatteryExecutor.Run(d, TimeSpan.FromSeconds(5))).ToList();
        Assert.All(results, r => Assert.True(r.Passed, $"{r.DefinitionName}: {r.FailureReason}"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void ReverseGenerator_BinaryTree_SplitRules_ProduceSameShape(int depth)
    {
        var definitions = ReverseRuleGenerator.GenerateBinaryTree(depth).ToList();
        var splitDefs = definitions.Where(d => d.Name.Contains("Split", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(splitDefs);
        // Split should have 2*depth rules (GoLeft + GoRight per depth level)
        var baseSplit = splitDefs.First(d => d.Name.EndsWith("_Split", StringComparison.Ordinal));
        Assert.Equal(2 * depth, baseSplit.Rules.Length);

        var results = splitDefs.Select(d => TestBatteryExecutor.Run(d, TimeSpan.FromSeconds(5))).ToList();
        Assert.All(results, r => Assert.True(r.Passed, $"{r.DefinitionName}: {r.FailureReason}"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ReverseGenerator_Diamond_SplitConverge_ProduceSameShape(int branchCount)
    {
        var definitions = ReverseRuleGenerator.GenerateDiamond(branchCount).ToList();
        var splitDefs = definitions.Where(d => d.Name.Contains("SplitConverge", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(splitDefs);
        // Split should have branchCount Branch rules + branchCount Converge rules
        var baseSplit = splitDefs.First(d => d.Name.EndsWith("_SplitConverge", StringComparison.Ordinal));
        Assert.Equal(branchCount * 2, baseSplit.Rules.Length);
        // All Converge rules share the same name
        var convergeRules = baseSplit.Rules.Where(r => r.GetName() == "Converge").ToList();
        Assert.Equal(branchCount, convergeRules.Count);

        var results = splitDefs.Select(d => TestBatteryExecutor.Run(d, TimeSpan.FromSeconds(5))).ToList();
        Assert.All(results, r => Assert.True(r.Passed, $"{r.DefinitionName}: {r.FailureReason}"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void ReverseGenerator_FullyConnected_SplitRules_ProduceSameShape(int nodeCount)
    {
        var definitions = ReverseRuleGenerator.GenerateFullyConnected(nodeCount).ToList();
        var splitDefs = definitions.Where(d => d.Name.Contains("Split", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(splitDefs);
        // Split should have nodeCount * (nodeCount - 1) rules total
        // (one per source-target pair, grouped by target with same name)
        var baseSplit = splitDefs.First(d => d.Name.EndsWith("_Split", StringComparison.Ordinal));
        Assert.Equal(nodeCount * (nodeCount - 1), baseSplit.Rules.Length);
        // Each GoToN name appears (nodeCount - 1) times
        for (int t = 0; t < nodeCount; t++)
        {
            string expectedName = $"GoTo{t.ToString(CultureInfo.InvariantCulture)}";
            int count = baseSplit.Rules.Count(r => r.GetName() == expectedName);
            Assert.Equal(nodeCount - 1, count);
        }

        var results = splitDefs.Select(d => TestBatteryExecutor.Run(d, TimeSpan.FromSeconds(5))).ToList();
        Assert.All(results, r => Assert.True(r.Passed, $"{r.DefinitionName}: {r.FailureReason}"));
    }

    [Fact]
    public void RunAll_AllReverseGeneratedWithNewVariations_PassAllOracles()
    {
        // Comprehensive sweep of all shapes including new ordering and split variations
        var definitions = ReverseRuleGenerator.GenerateAllShapes().ToList();
        var results = TestBatteryExecutor.RunAll(definitions, TimeSpan.FromSeconds(5)).ToList();

        var failures = results.Where(r => !r.Passed).ToList();

        Assert.True(failures.Count == 0,
            $"{failures.Count.ToString(CultureInfo.InvariantCulture)} of {results.Count.ToString(CultureInfo.InvariantCulture)} definitions failed:\n" +
            string.Join("\n", failures.Select(f => $"  - {f.DefinitionName}: {f.FailureReason}")));
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
