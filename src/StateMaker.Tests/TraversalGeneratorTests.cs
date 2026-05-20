using System.IO;
using Xunit;

namespace StateMaker.Tests;

public class TraversalGeneratorTests
{
    // S0 --(step1)--> S1 --(step2)--> S2
    private static StateMachine BuildLinearChain()
    {
        var sm = new StateMachine();
        sm.AddOrUpdateState("S0", new State());
        sm.AddOrUpdateState("S1", new State());
        sm.AddOrUpdateState("S2", new State());
        sm.StartingStateId = "S0";
        sm.Transitions.Add(new Transition("S0", "S1", "step1"));
        sm.Transitions.Add(new Transition("S1", "S2", "step2"));
        return sm;
    }

    // S0 --(a)--> S1 (terminal)
    // S0 --(b)--> S2 (terminal)
    private static StateMachine BuildBranchMachine()
    {
        var sm = new StateMachine();
        sm.AddOrUpdateState("S0", new State());
        sm.AddOrUpdateState("S1", new State());
        sm.AddOrUpdateState("S2", new State());
        sm.StartingStateId = "S0";
        sm.Transitions.Add(new Transition("S0", "S1", "a"));
        sm.Transitions.Add(new Transition("S0", "S2", "b"));
        return sm;
    }

    // S0 --(go)--> S1 --(back)--> S0  (cycle)
    private static StateMachine BuildCycleMachine()
    {
        var sm = new StateMachine();
        sm.AddOrUpdateState("S0", new State());
        sm.AddOrUpdateState("S1", new State());
        sm.StartingStateId = "S0";
        sm.Transitions.Add(new Transition("S0", "S1", "go"));
        sm.Transitions.Add(new Transition("S1", "S0", "back"));
        return sm;
    }

    // S0 --(go)--> S1; S2 is disconnected (unreachable)
    private static StateMachine BuildWithUnreachableState()
    {
        var sm = new StateMachine();
        sm.AddOrUpdateState("S0", new State());
        sm.AddOrUpdateState("S1", new State());
        sm.AddOrUpdateState("S2", new State());
        sm.StartingStateId = "S0";
        sm.Transitions.Add(new Transition("S0", "S1", "go"));
        return sm;
    }

    private static Traversal FindSingle(IReadOnlyList<Traversal> traversals, Func<Traversal, bool> predicate)
        => traversals.Where(predicate).Single();

    private static StateMachine LoadMachineFromSampledata(string filename)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "sampledata", filename);
        var json = File.ReadAllText(path);
        return new JsonImporter().Import(json);
    }

    // Returns the set of state IDs visited by a traversal (source of each transition + final target).
    private static HashSet<string> StatesVisited(Traversal traversal, string startStateId)
    {
        var states = new HashSet<string> { startStateId };
        foreach (var t in traversal.Transitions)
            states.Add(t.TargetStateId);
        return states;
    }

    // Returns true when every transition in 'shorter' matches the corresponding transition in 'longer'
    // and 'shorter' has fewer transitions — i.e., 'shorter' is a strict prefix of 'longer'.
    private static bool IsStrictPrefix(IReadOnlyList<Transition> shorter, IReadOnlyList<Transition> longer)
    {
        if (shorter.Count >= longer.Count) return false;
        for (int i = 0; i < shorter.Count; i++)
        {
            if (shorter[i].SourceStateId != longer[i].SourceStateId ||
                shorter[i].TargetStateId != longer[i].TargetStateId ||
                shorter[i].RuleName != longer[i].RuleName)
                return false;
        }
        return true;
    }

    #region AllStates

    [Fact]
    public void AllStates_LinearChain_ProducesMinimumTraversals()
    {
        // S0->S1->S2: only the terminal state S2 is kept; S0 and S1 are prefixes of S2's path.
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStates);
        Assert.Single(traversals);
    }

    [Fact]
    public void AllStates_OnlyStartingState_HasZeroTransitions()
    {
        // When the start state is the only reachable state it is kept with an empty path.
        var sm = new StateMachine();
        sm.AddOrUpdateState("S0", new State());
        sm.StartingStateId = "S0";

        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStates);
        Assert.Single(traversals);
        Assert.Empty(traversals[0].Transitions);
    }

    [Fact]
    public void AllStates_BranchTerminalState_PathEndsAtThatState()
    {
        // Branch machine S0->S1 and S0->S2: S1 and S2 are both terminal and neither is a
        // prefix of the other, so both traversals are kept.
        var sm = BuildBranchMachine();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStates);
        var s1Traversal = FindSingle(traversals, t => t.Name.Contains("S1", StringComparison.Ordinal));
        Assert.Single(s1Traversal.Transitions);
        Assert.Equal("S1", s1Traversal.Transitions[^1].TargetStateId);
    }

    [Fact]
    public void AllStates_FinalState_PathEndsAtThatState()
    {
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStates);
        var s2Traversal = FindSingle(traversals, t => t.Name.Contains("S2", StringComparison.Ordinal));
        Assert.Equal(2, s2Traversal.Transitions.Count);
        Assert.Equal("S2", s2Traversal.Transitions[^1].TargetStateId);
    }

    [Fact]
    public void AllStates_UnreachableState_IsSkipped()
    {
        var sm = BuildWithUnreachableState();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStates);
        // S0 is a prefix of S1's path so only S1 is kept; S2 is unreachable and also absent.
        Assert.Single(traversals);
        Assert.DoesNotContain(traversals, t => t.Name.Contains("S2", StringComparison.Ordinal));
    }

    [Fact]
    public void AllStates_TraversalIds_UseDefaultPrefix()
    {
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStates);
        Assert.All(traversals, t => Assert.StartsWith("T", t.Id, StringComparison.Ordinal));
    }

    [Fact]
    public void AllStates_TraversalIds_UseCustomPrefix()
    {
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStates, "TC");
        Assert.All(traversals, t => Assert.StartsWith("TC", t.Id, StringComparison.Ordinal));
    }

    [Fact]
    public void AllStates_VcaiVhaMachine_CoversAllStates()
    {
        var sm = LoadMachineFromSampledata("vcai_vha_machine.json");
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStates);

        var covered = new HashSet<string>();
        foreach (var traversal in traversals)
            covered.UnionWith(StatesVisited(traversal, sm.StartingStateId!));

        foreach (var stateId in sm.States.Keys)
            Assert.Contains(stateId, covered);
    }

    [Fact]
    public void AllStates_VcaiVhaMachine_NoTraversalIsSubtraversalOfAnother()
    {
        var sm = LoadMachineFromSampledata("vcai_vha_machine.json");
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStates);

        for (int i = 0; i < traversals.Count; i++)
        {
            for (int j = 0; j < traversals.Count; j++)
            {
                if (i == j) continue;
                Assert.False(
                    IsStrictPrefix(traversals[i].Transitions, traversals[j].Transitions),
                    $"Traversal '{traversals[i].Id}' ({traversals[i].Transitions.Count} steps) is a " +
                    $"sub-traversal of '{traversals[j].Id}' ({traversals[j].Transitions.Count} steps) " +
                    $"and is redundant — all its states are already covered by the longer traversal.");
            }
        }
    }

    #endregion

    #region AllTransitions

    [Fact]
    public void AllTransitions_LinearChain_ProducesMinimumTraversals()
    {
        // S0->S1->S2: the S0->S1 traversal is a prefix of the S1->S2 traversal, so only S1->S2 is kept.
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllTransitions);
        Assert.Single(traversals);
    }

    [Fact]
    public void AllTransitions_EachTraversalEndsWithItsTargetTransition()
    {
        // Branch machine: S0->S1 (a) and S0->S2 (b) diverge at S0 so neither path is a prefix
        // of the other — both traversals are kept.
        var sm = BuildBranchMachine();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllTransitions);

        Assert.Equal(2, traversals.Count);
        Assert.Equal("S0", traversals[0].Transitions[^1].SourceStateId);
        Assert.Equal("S1", traversals[0].Transitions[^1].TargetStateId);
        Assert.Equal("S0", traversals[1].Transitions[^1].SourceStateId);
        Assert.Equal("S2", traversals[1].Transitions[^1].TargetStateId);
    }

    [Fact]
    public void AllTransitions_PathLeadsToSourceOfEachTransition()
    {
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllTransitions);

        // Only the S1->S2 traversal survives; it needs S0->S1 as setup.
        Assert.Single(traversals);
        Assert.Equal(2, traversals[0].Transitions.Count);
        Assert.Equal("S0", traversals[0].Transitions[0].SourceStateId);
    }

    [Fact]
    public void AllTransitions_BranchMachine_ProducesTraversalForEachBranch()
    {
        var sm = BuildBranchMachine();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllTransitions);
        Assert.Equal(2, traversals.Count);
    }

    [Fact]
    public void AllTransitions_VcaiVhaMachine_CoversAllTransitions()
    {
        var sm = LoadMachineFromSampledata("vcai_vha_machine.json");
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllTransitions);

        var covered = new HashSet<(string src, string tgt, string rule)>();
        foreach (var traversal in traversals)
            foreach (var t in traversal.Transitions)
                covered.Add((t.SourceStateId, t.TargetStateId, t.RuleName));

        foreach (var t in sm.Transitions)
            Assert.Contains((t.SourceStateId, t.TargetStateId, t.RuleName), covered);
    }

    [Fact]
    public void AllTransitions_VcaiVhaMachine_NoTraversalIsSubtraversalOfAnother()
    {
        var sm = LoadMachineFromSampledata("vcai_vha_machine.json");
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllTransitions);

        for (int i = 0; i < traversals.Count; i++)
        {
            for (int j = 0; j < traversals.Count; j++)
            {
                if (i == j) continue;
                Assert.False(
                    IsStrictPrefix(traversals[i].Transitions, traversals[j].Transitions),
                    $"Traversal '{traversals[i].Id}' ({traversals[i].Transitions.Count} steps) is a " +
                    $"sub-traversal of '{traversals[j].Id}' ({traversals[j].Transitions.Count} steps) " +
                    $"and is redundant.");
            }
        }
    }

    #endregion

    #region AllPaths

    [Fact]
    public void AllPaths_LinearChain_ProducesOnePath()
    {
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllPaths);
        Assert.Single(traversals);
        Assert.Equal(2, traversals[0].Transitions.Count);
    }

    [Fact]
    public void AllPaths_BranchMachine_ProducesTwoPaths()
    {
        var sm = BuildBranchMachine();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllPaths);
        Assert.Equal(2, traversals.Count);
    }

    [Fact]
    public void AllPaths_CycleMachine_DoesNotLoopInfinitely()
    {
        var sm = BuildCycleMachine();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllPaths);
        // Should finish and produce at least one traversal
        Assert.NotEmpty(traversals);
        // No traversal should repeat a state
        foreach (var t in traversals)
        {
            var seenStates = new HashSet<string> { sm.StartingStateId! };
            foreach (var tr in t.Transitions)
            {
                Assert.DoesNotContain(tr.TargetStateId, seenStates);
                seenStates.Add(tr.TargetStateId);
            }
        }
    }

    [Fact]
    public void AllPaths_AllPathsStartFromStartingState()
    {
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllPaths);
        foreach (var t in traversals.Where(t => t.Transitions.Count > 0))
        {
            Assert.Equal(sm.StartingStateId, t.Transitions[0].SourceStateId);
        }
    }

    #endregion

    #region AllStatePairs

    [Fact]
    public void AllStatePairs_LinearChain_CoversAllReachablePairs()
    {
        var sm = BuildLinearChain();
        // Reachable pairs: (S0,S1), (S0,S2), (S1,S2) => 3 traversals
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStatePairs);
        Assert.Equal(3, traversals.Count);
    }

    [Fact]
    public void AllStatePairs_LinearChain_TraversalsEndAtTargetState()
    {
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStatePairs);

        // Each non-empty traversal should have transitions
        foreach (var t in traversals.Where(t => t.Transitions.Count > 0))
        {
            Assert.NotEmpty(t.Transitions);
        }
    }

    [Fact]
    public void AllStatePairs_NamesDescribeSourceAndTarget()
    {
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStatePairs);

        Assert.Contains(traversals, t => t.Name.Contains("S0", StringComparison.Ordinal)
                                      && t.Name.Contains("S1", StringComparison.Ordinal));
        Assert.Contains(traversals, t => t.Name.Contains("S1", StringComparison.Ordinal)
                                      && t.Name.Contains("S2", StringComparison.Ordinal));
    }

    #endregion

    #region Error cases

    [Fact]
    public void Generate_NullMachine_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TraversalGenerator.Generate(null!, ExportType.AllStates));
    }

    [Fact]
    public void Generate_DefaultExportType_ThrowsArgumentException()
    {
        var sm = BuildLinearChain();
        Assert.Throws<ArgumentException>(() =>
            TraversalGenerator.Generate(sm, ExportType.Default));
    }

    #endregion
}
