using System.Globalization;

namespace StateMaker.Tests;

public static class ReverseRuleGenerator
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

    public static IEnumerable<BuildDefinition> GenerateChain(int length)
    {
        // Chain: S0 -> S1 -> ... -> S(length)
        // States: length + 1, Transitions: length, MaxDepth: length
        var expected = new ExpectedShapeInfo(length + 1, length, length);
        var initial = new State();
        initial.Variables["step"] = 0;

        IRule[] baseRules =
        {
            new FuncRule("StepForward",
                s => (int)s.Variables["step"]! < length,
                s => { var c = s.Clone(); c.Variables["step"] = (int)c.Variables["step"]! + 1; return c; })
        };

        string name = $"Chain({length.ToString(CultureInfo.InvariantCulture)})";
        var config = new BuilderConfig();

        yield return new BuildDefinition($"{name}_Base", initial.Clone(), baseRules, config, expected);
        yield return new BuildDefinition($"{name}_WithNonTrigger", initial.Clone(),
            AddNonTriggeringRule(baseRules), config, expected);
        yield return new BuildDefinition($"{name}_Reversed", initial.Clone(),
            ReverseRules(AddNonTriggeringRule(baseRules)), config, expected);

        // Variation: shuffled rule ordering
        foreach (var (shuffleName, shuffled) in ShuffleRules(AddNonTriggeringRule(baseRules)))
            yield return new BuildDefinition($"{name}_{shuffleName}", initial.Clone(), shuffled, config, expected);

        // Variation: split single rule into per-state specialized rules with same GetName
        if (length > 1)
        {
            var splitRules = SplitChainRule(length);
            yield return new BuildDefinition($"{name}_Split", initial.Clone(), splitRules, config, expected);
            yield return new BuildDefinition($"{name}_SplitReversed", initial.Clone(),
                ReverseRules(splitRules), config, expected);
            yield return new BuildDefinition($"{name}_SplitShuffled", initial.Clone(),
                InterleaveWithNonTrigger(splitRules), config, expected);
        }
    }

    public static IEnumerable<BuildDefinition> GenerateCycle(int length)
    {
        // Cycle: modular arithmetic producing length states, each with 1 transition + back-edge
        // States: length, Transitions: length, MaxDepth: length - 1
        var expected = new ExpectedShapeInfo(length, length, length - 1);
        var initial = new State();
        initial.Variables["phase"] = 0;

        IRule[] baseRules =
        {
            new FuncRule("CycleStep",
                _ => true,
                s => { var c = s.Clone(); c.Variables["phase"] = ((int)c.Variables["phase"]! + 1) % length; return c; })
        };

        string name = $"Cycle({length.ToString(CultureInfo.InvariantCulture)})";
        var config = new BuilderConfig();

        yield return new BuildDefinition($"{name}_Base", initial.Clone(), baseRules, config, expected);
        yield return new BuildDefinition($"{name}_WithNonTrigger", initial.Clone(),
            AddNonTriggeringRule(baseRules), config, expected);
        yield return new BuildDefinition($"{name}_Reversed", initial.Clone(),
            ReverseRules(AddNonTriggeringRule(baseRules)), config, expected);

        foreach (var (shuffleName, shuffled) in ShuffleRules(AddNonTriggeringRule(baseRules)))
            yield return new BuildDefinition($"{name}_{shuffleName}", initial.Clone(), shuffled, config, expected);

        // Variation: split into per-state rules (each handles one phase value)
        var splitRules = SplitCycleRule(length);
        yield return new BuildDefinition($"{name}_Split", initial.Clone(), splitRules, config, expected);
        yield return new BuildDefinition($"{name}_SplitReversed", initial.Clone(),
            ReverseRules(splitRules), config, expected);
        yield return new BuildDefinition($"{name}_SplitShuffled", initial.Clone(),
            InterleaveWithNonTrigger(splitRules), config, expected);
    }

    public static IEnumerable<BuildDefinition> GenerateChainThenCycle(int chainLength, int cycleLength)
    {
        int totalStates = chainLength + cycleLength;
        int totalTransitions = chainLength + cycleLength;
        var expected = new ExpectedShapeInfo(totalStates, totalTransitions, chainLength + cycleLength - 1);
        var initial = new State();
        initial.Variables["step"] = 0;

        IRule[] baseRules =
        {
            new FuncRule("Advance",
                _ => true,
                s =>
                {
                    var c = s.Clone();
                    int step = (int)c.Variables["step"]!;
                    if (step < chainLength)
                    {
                        c.Variables["step"] = step + 1;
                    }
                    else
                    {
                        int cyclePos = step - chainLength;
                        int nextPos = (cyclePos + 1) % cycleLength;
                        c.Variables["step"] = chainLength + nextPos;
                    }
                    return c;
                })
        };

        string name = $"ChainCycle({chainLength.ToString(CultureInfo.InvariantCulture)},{cycleLength.ToString(CultureInfo.InvariantCulture)})";
        var config = new BuilderConfig();

        yield return new BuildDefinition($"{name}_Base", initial.Clone(), baseRules, config, expected);
        yield return new BuildDefinition($"{name}_WithNonTrigger", initial.Clone(),
            AddNonTriggeringRule(baseRules), config, expected);

        // Variation: split into per-state rules
        var splitRules = SplitChainThenCycleRule(chainLength, cycleLength);
        yield return new BuildDefinition($"{name}_Split", initial.Clone(), splitRules, config, expected);
        yield return new BuildDefinition($"{name}_SplitReversed", initial.Clone(),
            ReverseRules(splitRules), config, expected);
    }

    public static IEnumerable<BuildDefinition> GenerateBinaryTree(int depth)
    {
        int stateCount = (1 << (depth + 1)) - 1;
        int transitionCount = stateCount - 1;
        var expected = new ExpectedShapeInfo(stateCount, transitionCount, depth);
        var initial = new State();
        initial.Variables["path"] = "";

        IRule[] baseRules =
        {
            new FuncRule("GoLeft",
                s => ((string)s.Variables["path"]!).Length < depth,
                s => { var c = s.Clone(); c.Variables["path"] = (string)c.Variables["path"]! + "L"; return c; }),
            new FuncRule("GoRight",
                s => ((string)s.Variables["path"]!).Length < depth,
                s => { var c = s.Clone(); c.Variables["path"] = (string)c.Variables["path"]! + "R"; return c; })
        };

        string name = $"BinaryTree({depth.ToString(CultureInfo.InvariantCulture)})";
        var config = new BuilderConfig();

        yield return new BuildDefinition($"{name}_Base", initial.Clone(), baseRules, config, expected);
        yield return new BuildDefinition($"{name}_WithNonTrigger", initial.Clone(),
            AddNonTriggeringRule(baseRules), config, expected);
        yield return new BuildDefinition($"{name}_Reversed", initial.Clone(),
            ReverseRules(baseRules), config, expected);

        foreach (var (shuffleName, shuffled) in ShuffleRules(baseRules))
            yield return new BuildDefinition($"{name}_{shuffleName}", initial.Clone(), shuffled, config, expected);

        // Variation: split GoLeft into per-depth specialized rules, all named "GoLeft"
        if (depth > 1)
        {
            var splitRules = SplitBinaryTreeRules(depth);
            yield return new BuildDefinition($"{name}_Split", initial.Clone(), splitRules, config, expected);
            yield return new BuildDefinition($"{name}_SplitReversed", initial.Clone(),
                ReverseRules(splitRules), config, expected);
        }
    }

    public static IEnumerable<BuildDefinition> GenerateDiamond(int branchCount)
    {
        int stateCount = branchCount + 2;
        int transitionCount = branchCount * 2;
        var expected = new ExpectedShapeInfo(stateCount, transitionCount, 2);
        var initial = new State();
        initial.Variables["phase"] = "root";
        initial.Variables["branch"] = -1;

        var rules = new List<IRule>();
        for (int i = 0; i < branchCount; i++)
        {
            int branchId = i;
            rules.Add(new FuncRule($"Branch{branchId.ToString(CultureInfo.InvariantCulture)}",
                s => (string)s.Variables["phase"]! == "root",
                s =>
                {
                    var c = s.Clone();
                    c.Variables["phase"] = "branch";
                    c.Variables["branch"] = branchId;
                    return c;
                }));
        }
        rules.Add(new FuncRule("Converge",
            s => (string)s.Variables["phase"]! == "branch",
            s =>
            {
                var c = s.Clone();
                c.Variables["phase"] = "end";
                c.Variables["branch"] = -1;
                return c;
            }));

        string name = $"Diamond({branchCount.ToString(CultureInfo.InvariantCulture)})";
        var config = new BuilderConfig();
        IRule[] baseRules = rules.ToArray();

        yield return new BuildDefinition($"{name}_Base", initial.Clone(), baseRules, config, expected);
        yield return new BuildDefinition($"{name}_WithNonTrigger", initial.Clone(),
            AddNonTriggeringRule(baseRules), config, expected);
        yield return new BuildDefinition($"{name}_Reversed", initial.Clone(),
            ReverseRules(baseRules), config, expected);

        foreach (var (shuffleName, shuffled) in ShuffleRules(baseRules))
            yield return new BuildDefinition($"{name}_{shuffleName}", initial.Clone(), shuffled, config, expected);

        // Variation: split Converge into per-branch specialized rules, all named "Converge"
        var splitRules = SplitDiamondConverge(branchCount);
        yield return new BuildDefinition($"{name}_SplitConverge", initial.Clone(), splitRules, config, expected);
        yield return new BuildDefinition($"{name}_SplitConvergeReversed", initial.Clone(),
            ReverseRules(splitRules), config, expected);
    }

    public static IEnumerable<BuildDefinition> GenerateFullyConnected(int nodeCount)
    {
        int transitionCount = nodeCount * (nodeCount - 1);
        var expected = new ExpectedShapeInfo(nodeCount, transitionCount, nodeCount > 1 ? 1 : 0);
        var initial = new State();
        initial.Variables["node"] = 0;

        var rules = new List<IRule>();
        for (int target = 0; target < nodeCount; target++)
        {
            int t = target;
            rules.Add(new FuncRule($"GoTo{t.ToString(CultureInfo.InvariantCulture)}",
                s => (int)s.Variables["node"]! != t,
                s =>
                {
                    var c = s.Clone();
                    c.Variables["node"] = t;
                    return c;
                }));
        }

        string name = $"FullyConnected({nodeCount.ToString(CultureInfo.InvariantCulture)})";
        var config = new BuilderConfig();
        IRule[] baseRules = rules.ToArray();

        yield return new BuildDefinition($"{name}_Base", initial.Clone(), baseRules, config, expected);
        yield return new BuildDefinition($"{name}_WithNonTrigger", initial.Clone(),
            AddNonTriggeringRule(baseRules), config, expected);
        yield return new BuildDefinition($"{name}_Reversed", initial.Clone(),
            ReverseRules(baseRules), config, expected);

        foreach (var (shuffleName, shuffled) in ShuffleRules(baseRules))
            yield return new BuildDefinition($"{name}_{shuffleName}", initial.Clone(), shuffled, config, expected);

        // Variation: split each GoToN into per-source specialized rules, all with same name
        if (nodeCount >= 2)
        {
            var splitRules = SplitFullyConnectedRules(nodeCount);
            yield return new BuildDefinition($"{name}_Split", initial.Clone(), splitRules, config, expected);
            yield return new BuildDefinition($"{name}_SplitReversed", initial.Clone(),
                ReverseRules(splitRules), config, expected);
        }
    }

    public static IEnumerable<BuildDefinition> GenerateAllShapes()
    {
        // Chains
        foreach (int len in new[] { 1, 3, 5, 10 })
            foreach (var def in GenerateChain(len))
                yield return def;

        // Cycles
        foreach (int len in new[] { 2, 3, 5 })
            foreach (var def in GenerateCycle(len))
                yield return def;

        // Chain-then-cycle
        foreach (var (cl, cyc) in new[] { (1, 2), (2, 3), (3, 3) })
            foreach (var def in GenerateChainThenCycle(cl, cyc))
                yield return def;

        // Binary trees
        foreach (int d in new[] { 1, 2, 3 })
            foreach (var def in GenerateBinaryTree(d))
                yield return def;

        // Diamonds
        foreach (int b in new[] { 2, 3, 4 })
            foreach (var def in GenerateDiamond(b))
                yield return def;

        // Fully connected
        foreach (int k in new[] { 2, 3, 4 })
            foreach (var def in GenerateFullyConnected(k))
                yield return def;
    }

    #region Variation helpers

    private static IRule[] AddNonTriggeringRule(IRule[] rules)
    {
        var extended = new IRule[rules.Length + 1];
        Array.Copy(rules, extended, rules.Length);
        extended[rules.Length] = new FuncRule("NeverFires", _ => false, s => s.Clone());
        return extended;
    }

    private static IRule[] ReverseRules(IRule[] rules)
    {
        var reversed = new IRule[rules.Length];
        Array.Copy(rules, reversed, rules.Length);
        Array.Reverse(reversed);
        return reversed;
    }

    /// <summary>
    /// Produces additional shuffled orderings of the rules array.
    /// For arrays with 2+ elements, yields a rotation and an interleaved-with-non-trigger ordering.
    /// </summary>
    private static IEnumerable<(string Name, IRule[] Rules)> ShuffleRules(IRule[] rules)
    {
        if (rules.Length < 2)
            yield break;

        // Rotation: move first element to end
        var rotated = new IRule[rules.Length];
        Array.Copy(rules, 1, rotated, 0, rules.Length - 1);
        rotated[rules.Length - 1] = rules[0];
        yield return ("Rotated", rotated);

        if (rules.Length >= 3)
        {
            // Interleave odd/even indices
            var interleaved = new IRule[rules.Length];
            int idx = 0;
            for (int i = 0; i < rules.Length; i += 2)
                interleaved[idx++] = rules[i];
            for (int i = 1; i < rules.Length; i += 2)
                interleaved[idx++] = rules[i];
            yield return ("Interleaved", interleaved);
        }
    }

    /// <summary>
    /// Inserts non-triggering rules between existing rules.
    /// </summary>
    private static IRule[] InterleaveWithNonTrigger(IRule[] rules)
    {
        var result = new List<IRule>();
        for (int i = 0; i < rules.Length; i++)
        {
            result.Add(rules[i]);
            if (i < rules.Length - 1)
                result.Add(new FuncRule("NeverFires", _ => false, s => s.Clone()));
        }
        return result.ToArray();
    }

    #endregion

    #region Rule split generators

    /// <summary>
    /// Splits a chain rule "step < length" into N individual rules, one per step value.
    /// Each rule fires only when step == specificValue and all share the same GetName().
    /// </summary>
    private static IRule[] SplitChainRule(int length)
    {
        var rules = new IRule[length];
        for (int i = 0; i < length; i++)
        {
            int stepValue = i;
            rules[i] = new FuncRule("StepForward",
                s => (int)s.Variables["step"]! == stepValue,
                s => { var c = s.Clone(); c.Variables["step"] = stepValue + 1; return c; });
        }
        return rules;
    }

    /// <summary>
    /// Splits a cycle rule into N individual rules, one per phase value.
    /// Each rule fires only when phase == specificValue and all share the same GetName().
    /// </summary>
    private static IRule[] SplitCycleRule(int length)
    {
        var rules = new IRule[length];
        for (int i = 0; i < length; i++)
        {
            int phaseValue = i;
            int nextPhase = (phaseValue + 1) % length;
            rules[i] = new FuncRule("CycleStep",
                s => (int)s.Variables["phase"]! == phaseValue,
                s => { var c = s.Clone(); c.Variables["phase"] = nextPhase; return c; });
        }
        return rules;
    }

    /// <summary>
    /// Splits a chain-then-cycle rule into individual per-step rules, all named "Advance".
    /// </summary>
    private static IRule[] SplitChainThenCycleRule(int chainLength, int cycleLength)
    {
        int totalSteps = chainLength + cycleLength;
        var rules = new IRule[totalSteps];
        for (int i = 0; i < totalSteps; i++)
        {
            int stepValue = i;
            int nextStep;
            if (stepValue < chainLength)
            {
                nextStep = stepValue + 1;
            }
            else
            {
                int cyclePos = stepValue - chainLength;
                nextStep = chainLength + ((cyclePos + 1) % cycleLength);
            }
            rules[i] = new FuncRule("Advance",
                s => (int)s.Variables["step"]! == stepValue,
                s => { var c = s.Clone(); c.Variables["step"] = nextStep; return c; });
        }
        return rules;
    }

    /// <summary>
    /// Splits GoLeft and GoRight into per-depth specialized rules.
    /// Each depth level gets its own GoLeft and GoRight rule, all sharing the original names.
    /// </summary>
    private static IRule[] SplitBinaryTreeRules(int depth)
    {
        var rules = new List<IRule>();
        for (int d = 0; d < depth; d++)
        {
            int targetLen = d;
            rules.Add(new FuncRule("GoLeft",
                s => ((string)s.Variables["path"]!).Length == targetLen,
                s => { var c = s.Clone(); c.Variables["path"] = (string)c.Variables["path"]! + "L"; return c; }));
            rules.Add(new FuncRule("GoRight",
                s => ((string)s.Variables["path"]!).Length == targetLen,
                s => { var c = s.Clone(); c.Variables["path"] = (string)c.Variables["path"]! + "R"; return c; }));
        }
        return rules.ToArray();
    }

    /// <summary>
    /// Splits the single Converge rule into N per-branch rules, all named "Converge".
    /// Each fires only when branch == specificBranchId.
    /// </summary>
    private static IRule[] SplitDiamondConverge(int branchCount)
    {
        var rules = new List<IRule>();
        // Branch rules (one per branch, each with unique name)
        for (int i = 0; i < branchCount; i++)
        {
            int branchId = i;
            rules.Add(new FuncRule($"Branch{branchId.ToString(CultureInfo.InvariantCulture)}",
                s => (string)s.Variables["phase"]! == "root",
                s =>
                {
                    var c = s.Clone();
                    c.Variables["phase"] = "branch";
                    c.Variables["branch"] = branchId;
                    return c;
                }));
        }
        // Split Converge into per-branch specialized rules, all named "Converge"
        for (int i = 0; i < branchCount; i++)
        {
            int branchId = i;
            rules.Add(new FuncRule("Converge",
                s => (string)s.Variables["phase"]! == "branch" && (int)s.Variables["branch"]! == branchId,
                s =>
                {
                    var c = s.Clone();
                    c.Variables["phase"] = "end";
                    c.Variables["branch"] = -1;
                    return c;
                }));
        }
        return rules.ToArray();
    }

    /// <summary>
    /// Splits each GoToN rule into per-source specialized rules.
    /// For each target T, creates one rule per source S (where S != T), all named "GoToT".
    /// </summary>
    private static IRule[] SplitFullyConnectedRules(int nodeCount)
    {
        var rules = new List<IRule>();
        for (int target = 0; target < nodeCount; target++)
        {
            int t = target;
            for (int source = 0; source < nodeCount; source++)
            {
                if (source == target) continue;
                int s = source;
                rules.Add(new FuncRule($"GoTo{t.ToString(CultureInfo.InvariantCulture)}",
                    state => (int)state.Variables["node"]! == s,
                    state =>
                    {
                        var c = state.Clone();
                        c.Variables["node"] = t;
                        return c;
                    }));
            }
        }
        return rules.ToArray();
    }

    #endregion
}
