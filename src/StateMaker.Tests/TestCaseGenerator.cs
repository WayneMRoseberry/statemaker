using System.Globalization;

namespace StateMaker.Tests;

public record BuildDefinition(string Name, State InitialState, IRule[] Rules, BuilderConfig Config);

public static class TestCaseGenerator
{
    private sealed class FuncRule : IRule
    {
        private readonly string _name;
        private readonly Func<State, bool> _isAvailable;
        private readonly Func<State, State> _execute;

        public FuncRule(string name, Func<State, bool> isAvailable, Func<State, State> execute)
        {
            _name = name;
            _isAvailable = isAvailable;
            _execute = execute;
        }

        public bool IsAvailable(State state) => _isAvailable(state);
        public State Execute(State state) => _execute(state);
        public string GetName() => _name;
    }

    public static IEnumerable<State> GenerateInitialStates()
    {
        // Empty state
        yield return new State();

        // Single string variable
        var s1 = new State();
        s1.Variables["status"] = "start";
        yield return s1;

        // Single int variable
        var s2 = new State();
        s2.Variables["counter"] = 0;
        yield return s2;

        // Single bool variable
        var s3 = new State();
        s3.Variables["flag"] = false;
        yield return s3;

        // Single double variable
        var s4 = new State();
        s4.Variables["value"] = 0.0;
        yield return s4;

        // Multiple variables (string + int + bool)
        var s5 = new State();
        s5.Variables["status"] = "init";
        s5.Variables["counter"] = 0;
        s5.Variables["active"] = true;
        yield return s5;

        // N int variables (1..5)
        for (int n = 1; n <= 5; n++)
        {
            var sn = new State();
            for (int i = 0; i < n; i++)
            {
                sn.Variables[$"v{i.ToString(CultureInfo.InvariantCulture)}"] = 0;
            }
            yield return sn;
        }
    }

    public static IEnumerable<BuilderConfig> GenerateConfigs()
    {
        int?[] maxStatesValues = { null, 0, -1, 1, 2, 3, 10 };
        int?[] maxDepthValues = { null, 0, -1, 1, 2, 3, 10 };
        ExplorationStrategy[] strategies = { ExplorationStrategy.BREADTHFIRSTSEARCH, ExplorationStrategy.DEPTHFIRSTSEARCH };

        // Pairwise: each MaxStates paired with a subset of MaxDepth values and both strategies
        // To keep it manageable, pair each MaxStates with null, 0, 1, 3 for MaxDepth, plus both strategies
        int?[] depthSubset = { null, 0, 1, 3 };

        foreach (var maxStates in maxStatesValues)
        {
            foreach (var maxDepth in depthSubset)
            {
                foreach (var strategy in strategies)
                {
                    yield return new BuilderConfig
                    {
                        MaxStates = maxStates,
                        MaxDepth = maxDepth,
                        ExplorationStrategy = strategy
                    };
                }
            }
        }

        // Also include edge-case depth values not in the subset paired with representative MaxStates
        int?[] statesSubset = { null, 1, 3 };
        int?[] extraDepths = { -1, 2, 10 };
        foreach (var maxStates in statesSubset)
        {
            foreach (var maxDepth in extraDepths)
            {
                yield return new BuilderConfig
                {
                    MaxStates = maxStates,
                    MaxDepth = maxDepth,
                    ExplorationStrategy = ExplorationStrategy.BREADTHFIRSTSEARCH
                };
            }
        }
    }

    public static IEnumerable<(string Name, IRule[] Rules)> GenerateRuleVariations()
    {
        // Empty rules
        yield return ("EmptyRules", Array.Empty<IRule>());

        // Never-available rule
        yield return ("NeverAvailable", new IRule[]
        {
            new FuncRule("NeverFires", _ => false, s => s.Clone())
        });

        // Sets variable to fixed value
        yield return ("SetsVariable", new IRule[]
        {
            new FuncRule("SetStatus",
                s => s.Variables.ContainsKey("status") && (string)s.Variables["status"]! != "done",
                s => { var c = s.Clone(); c.Variables["status"] = "done"; return c; })
        });

        // Adds a new variable
        yield return ("AddsVariable", new IRule[]
        {
            new FuncRule("AddFlag",
                s => !s.Variables.ContainsKey("added"),
                s => { var c = s.Clone(); c.Variables["added"] = true; return c; })
        });

        // Increments int variable
        yield return ("IncrementsInt", new IRule[]
        {
            new FuncRule("Increment",
                s => s.Variables.ContainsKey("counter") && (int)s.Variables["counter"]! < 10,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        });

        // Multiple rules: set + increment
        yield return ("SetAndIncrement", new IRule[]
        {
            new FuncRule("SetStatus",
                s => s.Variables.ContainsKey("status") && (string)s.Variables["status"]! == "init",
                s => { var c = s.Clone(); c.Variables["status"] = "running"; return c; }),
            new FuncRule("Increment",
                s => s.Variables.ContainsKey("counter") && (int)s.Variables["counter"]! < 5,
                s => { var c = s.Clone(); c.Variables["counter"] = (int)c.Variables["counter"]! + 1; return c; })
        });

        // Toggle bool
        yield return ("ToggleBool", new IRule[]
        {
            new FuncRule("Toggle",
                s => s.Variables.ContainsKey("flag"),
                s => { var c = s.Clone(); c.Variables["flag"] = !(bool)c.Variables["flag"]!; return c; })
        });
    }

    public static IEnumerable<BuildDefinition> GenerateAll()
    {
        var states = GenerateInitialStates().ToList();
        var configs = GenerateConfigs().ToList();
        var ruleVariations = GenerateRuleVariations().ToList();

        int index = 0;
        foreach (var initialState in states)
        {
            string stateDesc = DescribeState(initialState);
            foreach (var (ruleName, rules) in ruleVariations)
            {
                foreach (var config in configs)
                {
                    string configDesc = DescribeConfig(config);
                    string name = $"[{index.ToString(CultureInfo.InvariantCulture)}] {stateDesc} | {ruleName} | {configDesc}";
                    // Clone the initial state so each definition has its own copy
                    yield return new BuildDefinition(name, initialState.Clone(), rules, config);
                    index++;
                }
            }
        }
    }

    private static string DescribeState(State state)
    {
        if (state.Variables.Count == 0)
            return "Empty";
        var keys = string.Join(",", state.Variables.Keys.OrderBy(k => k, StringComparer.Ordinal));
        return $"Vars({keys})";
    }

    private static string DescribeConfig(BuilderConfig config)
    {
        string ms = config.MaxStates.HasValue ? config.MaxStates.Value.ToString(CultureInfo.InvariantCulture) : "null";
        string md = config.MaxDepth.HasValue ? config.MaxDepth.Value.ToString(CultureInfo.InvariantCulture) : "null";
        string strat = config.ExplorationStrategy == ExplorationStrategy.BREADTHFIRSTSEARCH ? "BFS" : "DFS";
        return $"MS={ms},MD={md},{strat}";
    }
}
