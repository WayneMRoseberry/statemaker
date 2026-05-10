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

    #region AllStates

    [Fact]
    public void AllStates_LinearChain_ProducesOneTraversalPerReachableState()
    {
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStates);
        Assert.Equal(3, traversals.Count);
    }

    [Fact]
    public void AllStates_StartingState_HasZeroTransitions()
    {
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllStates);
        var s0Traversal = FindSingle(traversals, t => t.Name.Contains("S0", StringComparison.Ordinal));
        Assert.Empty(s0Traversal.Transitions);
    }

    [Fact]
    public void AllStates_MiddleState_PathEndsAtThatState()
    {
        var sm = BuildLinearChain();
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
        // S0 and S1 are reachable; S2 is not
        Assert.Equal(2, traversals.Count);
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

    #endregion

    #region AllTransitions

    [Fact]
    public void AllTransitions_LinearChain_ProducesOneTraversalPerTransition()
    {
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllTransitions);
        Assert.Equal(2, traversals.Count);
    }

    [Fact]
    public void AllTransitions_EachTraversalEndsWithItsTargetTransition()
    {
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllTransitions);

        var t1 = traversals[0];
        Assert.Equal("S0", t1.Transitions[^1].SourceStateId);
        Assert.Equal("S1", t1.Transitions[^1].TargetStateId);

        var t2 = traversals[1];
        Assert.Equal("S1", t2.Transitions[^1].SourceStateId);
        Assert.Equal("S2", t2.Transitions[^1].TargetStateId);
    }

    [Fact]
    public void AllTransitions_PathLeadsToSourceOfEachTransition()
    {
        var sm = BuildLinearChain();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllTransitions);

        // S1->S2 traversal must pass through S0->S1 first
        var t2 = traversals[1];
        Assert.Equal(2, t2.Transitions.Count);
        Assert.Equal("S0", t2.Transitions[0].SourceStateId);
    }

    [Fact]
    public void AllTransitions_BranchMachine_ProducesTraversalForEachBranch()
    {
        var sm = BuildBranchMachine();
        var traversals = TraversalGenerator.Generate(sm, ExportType.AllTransitions);
        Assert.Equal(2, traversals.Count);
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
