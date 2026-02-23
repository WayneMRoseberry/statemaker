namespace StateMaker.Tests;

public class PathFilterTests
{
    #region Linear Chain

    [Fact]
    public void Filter_LinearChain_SelectedAtEnd_IncludesFullPath()
    {
        // S0 -> S1 -> S2 (selected)
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S2", "R2"));

        var selected = new HashSet<string> { "S2" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal(3, result.States.Count);
        Assert.Contains("S0", result.States.Keys);
        Assert.Contains("S1", result.States.Keys);
        Assert.Contains("S2", result.States.Keys);
        Assert.Equal(2, result.Transitions.Count);
        Assert.Equal("S0", result.StartingStateId);
    }

    [Fact]
    public void Filter_LinearChain_SelectedInMiddle_ExcludesAfterSelected()
    {
        // S0 -> S1 (selected) -> S2
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S2", "R2"));

        var selected = new HashSet<string> { "S1" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal(2, result.States.Count);
        Assert.Contains("S0", result.States.Keys);
        Assert.Contains("S1", result.States.Keys);
        Assert.DoesNotContain("S2", result.States.Keys);
        Assert.Single(result.Transitions);
    }

    #endregion

    #region Branching Paths

    [Fact]
    public void Filter_BranchingPaths_OnlyIncludesPathToSelected()
    {
        // S0 -> S1 -> S2 (selected)
        // S0 -> S3 -> S4
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.AddOrUpdateState("S3", new State());
        machine.AddOrUpdateState("S4", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S2", "R2"));
        machine.Transitions.Add(new Transition("S0", "S3", "R3"));
        machine.Transitions.Add(new Transition("S3", "S4", "R4"));

        var selected = new HashSet<string> { "S2" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal(3, result.States.Count);
        Assert.Contains("S0", result.States.Keys);
        Assert.Contains("S1", result.States.Keys);
        Assert.Contains("S2", result.States.Keys);
        Assert.DoesNotContain("S3", result.States.Keys);
        Assert.DoesNotContain("S4", result.States.Keys);
        Assert.Equal(2, result.Transitions.Count);
    }

    [Fact]
    public void Filter_BranchingPaths_MultipleSelectedOnDifferentBranches()
    {
        // S0 -> S1 -> S2 (selected)
        // S0 -> S3 -> S4 (selected)
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.AddOrUpdateState("S3", new State());
        machine.AddOrUpdateState("S4", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S2", "R2"));
        machine.Transitions.Add(new Transition("S0", "S3", "R3"));
        machine.Transitions.Add(new Transition("S3", "S4", "R4"));

        var selected = new HashSet<string> { "S2", "S4" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal(5, result.States.Count);
        Assert.Equal(4, result.Transitions.Count);
    }

    #endregion

    #region Convergent Paths

    [Fact]
    public void Filter_TwoBranchesConvergeOnSelectedState_BothBranchesIncluded()
    {
        // S0 -> S1 -> S3 (selected)
        // S0 -> S2 -> S3 (selected)
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.AddOrUpdateState("S3", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S3", "R2"));
        machine.Transitions.Add(new Transition("S0", "S2", "R3"));
        machine.Transitions.Add(new Transition("S2", "S3", "R4"));

        var selected = new HashSet<string> { "S3" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal(4, result.States.Count);
        Assert.Contains("S0", result.States.Keys);
        Assert.Contains("S1", result.States.Keys);
        Assert.Contains("S2", result.States.Keys);
        Assert.Contains("S3", result.States.Keys);
        Assert.Equal(4, result.Transitions.Count);
    }

    [Fact]
    public void Filter_TwoBranchesConvergeThenSelectedOneStepLater_BothBranchesIncluded()
    {
        // S0 -> S1 -> S3 -> S4 (selected)
        // S0 -> S2 -> S3 -> S4 (selected)
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.AddOrUpdateState("S3", new State());
        machine.AddOrUpdateState("S4", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S3", "R2"));
        machine.Transitions.Add(new Transition("S0", "S2", "R3"));
        machine.Transitions.Add(new Transition("S2", "S3", "R4"));
        machine.Transitions.Add(new Transition("S3", "S4", "R5"));

        var selected = new HashSet<string> { "S4" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal(5, result.States.Count);
        Assert.Contains("S0", result.States.Keys);
        Assert.Contains("S1", result.States.Keys);
        Assert.Contains("S2", result.States.Keys);
        Assert.Contains("S3", result.States.Keys);
        Assert.Contains("S4", result.States.Keys);
        Assert.Equal(5, result.Transitions.Count);
    }

    [Fact]
    public void Filter_ConvergentWithDeadBranch_DeadBranchExcluded()
    {
        // S0 -> S1 -> S3 -> S4 (selected)
        // S0 -> S2 -> S3 -> S4 (selected)
        // S0 -> S5 -> S6 (not selected, dead branch)
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.AddOrUpdateState("S3", new State());
        machine.AddOrUpdateState("S4", new State());
        machine.AddOrUpdateState("S5", new State());
        machine.AddOrUpdateState("S6", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S3", "R2"));
        machine.Transitions.Add(new Transition("S0", "S2", "R3"));
        machine.Transitions.Add(new Transition("S2", "S3", "R4"));
        machine.Transitions.Add(new Transition("S3", "S4", "R5"));
        machine.Transitions.Add(new Transition("S0", "S5", "R6"));
        machine.Transitions.Add(new Transition("S5", "S6", "R7"));

        var selected = new HashSet<string> { "S4" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal(5, result.States.Count);
        Assert.Contains("S0", result.States.Keys);
        Assert.Contains("S1", result.States.Keys);
        Assert.Contains("S2", result.States.Keys);
        Assert.Contains("S3", result.States.Keys);
        Assert.Contains("S4", result.States.Keys);
        Assert.DoesNotContain("S5", result.States.Keys);
        Assert.DoesNotContain("S6", result.States.Keys);
        Assert.Equal(5, result.Transitions.Count);
    }

    [Fact]
    public void Filter_ConvergentOnSelected_OneBranchHasNonSelectedSpur_SpurExcluded()
    {
        // S0 -> S1 -> S3 (selected)
        // S0 -> S2 -> S3 (selected)
        // S1 -> S5 (spur off convergent branch, not selected)
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.AddOrUpdateState("S3", new State());
        machine.AddOrUpdateState("S5", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S3", "R2"));
        machine.Transitions.Add(new Transition("S0", "S2", "R3"));
        machine.Transitions.Add(new Transition("S2", "S3", "R4"));
        machine.Transitions.Add(new Transition("S1", "S5", "R5"));

        var selected = new HashSet<string> { "S3" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal(4, result.States.Count);
        Assert.Contains("S0", result.States.Keys);
        Assert.Contains("S1", result.States.Keys);
        Assert.Contains("S2", result.States.Keys);
        Assert.Contains("S3", result.States.Keys);
        Assert.DoesNotContain("S5", result.States.Keys);
    }

    [Fact]
    public void Filter_ThreeBranchesConvergeOnSelected_AllThreeIncluded()
    {
        // S0 -> S1 -> S4 (selected)
        // S0 -> S2 -> S4 (selected)
        // S0 -> S3 -> S4 (selected)
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.AddOrUpdateState("S3", new State());
        machine.AddOrUpdateState("S4", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S4", "R2"));
        machine.Transitions.Add(new Transition("S0", "S2", "R3"));
        machine.Transitions.Add(new Transition("S2", "S4", "R4"));
        machine.Transitions.Add(new Transition("S0", "S3", "R5"));
        machine.Transitions.Add(new Transition("S3", "S4", "R6"));

        var selected = new HashSet<string> { "S4" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal(5, result.States.Count);
        Assert.Contains("S0", result.States.Keys);
        Assert.Contains("S1", result.States.Keys);
        Assert.Contains("S2", result.States.Keys);
        Assert.Contains("S3", result.States.Keys);
        Assert.Contains("S4", result.States.Keys);
        Assert.Equal(6, result.Transitions.Count);
    }

    #endregion

    #region Cycles

    [Fact]
    public void Filter_CycleInGraph_DoesNotInfiniteLoop()
    {
        // S0 -> S1 -> S2 (selected)
        // S1 -> S0 (back edge / cycle)
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S2", "R2"));
        machine.Transitions.Add(new Transition("S1", "S0", "R3"));

        var selected = new HashSet<string> { "S2" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal(3, result.States.Count);
        Assert.Contains("S0", result.States.Keys);
        Assert.Contains("S1", result.States.Keys);
        Assert.Contains("S2", result.States.Keys);
        // 3 transitions: S0->S1, S1->S2, and back edge S1->S0
        // (back edge included because both endpoints are path states)
        Assert.Equal(3, result.Transitions.Count);
    }

    #endregion

    #region Selected States in Sequence

    [Fact]
    public void Filter_TwoSelectedInSequence_IncludesPathThroughBoth()
    {
        // S0 -> S1 -> S2 (selected) -> S3 (selected)
        // S0 -> S4
        // Should include S0, S1, S2, S3 and transitions S0->S1, S1->S2, S2->S3
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.AddOrUpdateState("S3", new State());
        machine.AddOrUpdateState("S4", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S2", "R2"));
        machine.Transitions.Add(new Transition("S2", "S3", "R3"));
        machine.Transitions.Add(new Transition("S0", "S4", "R4"));

        var selected = new HashSet<string> { "S2", "S3" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal(4, result.States.Count);
        Assert.Contains("S0", result.States.Keys);
        Assert.Contains("S1", result.States.Keys);
        Assert.Contains("S2", result.States.Keys);
        Assert.Contains("S3", result.States.Keys);
        Assert.DoesNotContain("S4", result.States.Keys);
        Assert.Equal(3, result.Transitions.Count);
        Assert.Contains(result.Transitions, t => t.SourceStateId == "S0" && t.TargetStateId == "S1");
        Assert.Contains(result.Transitions, t => t.SourceStateId == "S1" && t.TargetStateId == "S2");
        Assert.Contains(result.Transitions, t => t.SourceStateId == "S2" && t.TargetStateId == "S3");
    }

    [Fact]
    public void Filter_SelectedReachableOnlyThroughAnotherSelected_BothIncluded()
    {
        // S0 -> S1 -> S2 (selected) -> S3 -> S4 (selected)
        // S4 is only reachable through S2
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.AddOrUpdateState("S3", new State());
        machine.AddOrUpdateState("S4", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S2", "R2"));
        machine.Transitions.Add(new Transition("S2", "S3", "R3"));
        machine.Transitions.Add(new Transition("S3", "S4", "R4"));

        var selected = new HashSet<string> { "S2", "S4" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal(5, result.States.Count);
        Assert.Contains("S0", result.States.Keys);
        Assert.Contains("S1", result.States.Keys);
        Assert.Contains("S2", result.States.Keys);
        Assert.Contains("S3", result.States.Keys);
        Assert.Contains("S4", result.States.Keys);
        Assert.Equal(4, result.Transitions.Count);
    }

    #endregion

    #region No Matches

    [Fact]
    public void Filter_NoSelectedStates_ReturnsEmptyMachine()
    {
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));

        var selected = new HashSet<string>();

        var result = new PathFilter(machine, selected).Filter();

        Assert.Empty(result.States);
        Assert.Empty(result.Transitions);
        Assert.Null(result.StartingStateId);
    }

    [Fact]
    public void Filter_SelectedStateNotReachable_ReturnsEmptyMachine()
    {
        // S0 -> S1, S2 is disconnected but selected
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));

        var selected = new HashSet<string> { "S2" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Empty(result.States);
        Assert.Empty(result.Transitions);
    }

    #endregion

    #region Starting State Inclusion

    [Fact]
    public void Filter_StartingStateIsSelected_IncludesOnlyStartingState()
    {
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));

        var selected = new HashSet<string> { "S0" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Single(result.States);
        Assert.Contains("S0", result.States.Keys);
        Assert.Equal("S0", result.StartingStateId);
        Assert.Empty(result.Transitions);
    }

    [Fact]
    public void Filter_StartingStateIsSelected_AndOtherSelectedReachable_IncludesPathToOther()
    {
        // S0 (selected) -> S1 -> S2 (selected)
        // S0 -> S3
        // Should include S0, S1, S2 and transitions S0->S1, S1->S2
        // Should exclude S3 (not on path to a selected state)
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.AddOrUpdateState("S3", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S2", "R2"));
        machine.Transitions.Add(new Transition("S0", "S3", "R3"));

        var selected = new HashSet<string> { "S0", "S2" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal(3, result.States.Count);
        Assert.Contains("S0", result.States.Keys);
        Assert.Contains("S1", result.States.Keys);
        Assert.Contains("S2", result.States.Keys);
        Assert.DoesNotContain("S3", result.States.Keys);
        Assert.Equal(2, result.Transitions.Count);
        Assert.Contains(result.Transitions, t => t.SourceStateId == "S0" && t.TargetStateId == "S1");
        Assert.Contains(result.Transitions, t => t.SourceStateId == "S1" && t.TargetStateId == "S2");
    }

    [Fact]
    public void Filter_StartingStateAlwaysIncluded_WhenPathExists()
    {
        // S0 -> S1 -> S2 (selected)
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S2", "R2"));

        var selected = new HashSet<string> { "S2" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Contains("S0", result.States.Keys);
        Assert.Equal("S0", result.StartingStateId);
    }

    #endregion

    #region States Not on Path Excluded

    [Fact]
    public void Filter_StatesNotOnPath_AreExcluded()
    {
        // S0 -> S1 -> S2 (selected)
        // S0 -> S3 (dead end, not selected)
        // S5 is disconnected
        var machine = new StateMachine();
        machine.AddOrUpdateState("S0", new State());
        machine.AddOrUpdateState("S1", new State());
        machine.AddOrUpdateState("S2", new State());
        machine.AddOrUpdateState("S3", new State());
        machine.AddOrUpdateState("S5", new State());
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));
        machine.Transitions.Add(new Transition("S1", "S2", "R2"));
        machine.Transitions.Add(new Transition("S0", "S3", "R3"));

        var selected = new HashSet<string> { "S2" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.DoesNotContain("S3", result.States.Keys);
        Assert.DoesNotContain("S5", result.States.Keys);
        Assert.Equal(3, result.States.Count);
    }

    #endregion

    #region Preserves State Data

    [Fact]
    public void Filter_PreservesStateVariablesAndAttributes()
    {
        var machine = new StateMachine();
        var s0 = new State();
        s0.Variables["Status"] = "Pending";
        var s1 = new State();
        s1.Variables["Status"] = "Approved";
        s1.Attributes["ranking"] = "high";
        machine.AddOrUpdateState("S0", s0);
        machine.AddOrUpdateState("S1", s1);
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "R1"));

        var selected = new HashSet<string> { "S1" };

        var result = new PathFilter(machine, selected).Filter();

        Assert.Equal("Pending", result.States["S0"].Variables["Status"]);
        Assert.Equal("Approved", result.States["S1"].Variables["Status"]);
        Assert.Equal("high", result.States["S1"].Attributes["ranking"]);
    }

    #endregion
}
