using Xunit;

namespace StateMaker.Tests;

public class VisualTraversalExporterTests
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

    #region Step count in traversal labels

    [Fact]
    public void DotExporter_AllStates_ClusterLabelIncludesStepCount()
    {
        // Branch machine: S1 and S2 are each one step from S0, so both labels read "1 steps".
        var sm = BuildBranchMachine();
        var output = new DotExporter().Export(sm, ExportType.AllStates);
        Assert.Contains("1 steps - Reach S1", output, StringComparison.Ordinal);
        Assert.Contains("1 steps - Reach S2", output, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_AllStates_SubgraphLabelIncludesStepCount()
    {
        var sm = BuildBranchMachine();
        var output = new MermaidExporter().Export(sm, ExportType.AllStates);
        Assert.Contains("1 steps - Reach S1", output, StringComparison.Ordinal);
        Assert.Contains("1 steps - Reach S2", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_AllStates_GraphLabelIncludesStepCount()
    {
        var sm = BuildBranchMachine();
        var output = new GraphMlExporter().Export(sm, ExportType.AllStates);
        Assert.Contains("1 steps - Reach S1", output, StringComparison.Ordinal);
        Assert.Contains("1 steps - Reach S2", output, StringComparison.Ordinal);
    }

    #endregion

    #region DotExporter traversal output

    [Fact]
    public void DotExporter_AllStates_ContainsDigraphBlock()
    {
        var sm = BuildLinearChain();
        var output = new DotExporter().Export(sm, ExportType.AllStates);
        Assert.Contains("digraph", output, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_AllStates_ContainsClusterPerTraversal()
    {
        // Branch machine produces 2 minimum traversals (S1 and S2) => 2 clusters.
        var sm = BuildBranchMachine();
        var output = new DotExporter().Export(sm, ExportType.AllStates);
        Assert.Contains("cluster_T1", output, StringComparison.Ordinal);
        Assert.Contains("cluster_T2", output, StringComparison.Ordinal);
        Assert.DoesNotContain("cluster_T3", output, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_AllStates_ClusterLabelsContainStateName()
    {
        var sm = BuildLinearChain();
        var output = new DotExporter().Export(sm, ExportType.AllStates);
        Assert.Contains("S0", output, StringComparison.Ordinal);
        Assert.Contains("S1", output, StringComparison.Ordinal);
        Assert.Contains("S2", output, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_AllTransitions_ContainsEdges()
    {
        var sm = BuildLinearChain();
        var output = new DotExporter().Export(sm, ExportType.AllTransitions);
        Assert.Contains("->", output, StringComparison.Ordinal);
        Assert.Contains("step1", output, StringComparison.Ordinal);
        Assert.Contains("step2", output, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_AllTransitions_ProducesTwoTraversals()
    {
        // Branch machine: S0->S1 and S0->S2 diverge so neither is a prefix — both kept.
        var sm = BuildBranchMachine();
        var output = new DotExporter().Export(sm, ExportType.AllTransitions);
        Assert.Contains("cluster_T1", output, StringComparison.Ordinal);
        Assert.Contains("cluster_T2", output, StringComparison.Ordinal);
        Assert.DoesNotContain("cluster_T3", output, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_Default_OutputUnchanged()
    {
        var sm = BuildLinearChain();
        var output = new DotExporter().Export(sm, ExportType.Default);
        Assert.Contains("digraph StateMachine", output, StringComparison.Ordinal);
        Assert.Contains("__start", output, StringComparison.Ordinal);
    }

    #endregion

    #region MermaidExporter traversal output

    [Fact]
    public void MermaidExporter_AllStates_ContainsFlowchart()
    {
        var sm = BuildLinearChain();
        var output = new MermaidExporter().Export(sm, ExportType.AllStates);
        Assert.Contains("flowchart TD", output, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_AllStates_ContainsSubgraphPerTraversal()
    {
        // Branch machine produces 2 minimum traversals => 2 subgraphs.
        var sm = BuildBranchMachine();
        var output = new MermaidExporter().Export(sm, ExportType.AllStates);
        var count = CountOccurrences(output, "subgraph");
        Assert.Equal(2, count);
    }

    [Fact]
    public void MermaidExporter_AllStates_SubgraphsHaveTraversalNames()
    {
        // Branch machine: S1 and S2 are the minimum set; S0 is filtered as a prefix of both.
        var sm = BuildBranchMachine();
        var output = new MermaidExporter().Export(sm, ExportType.AllStates);
        Assert.Contains("Reach S1", output, StringComparison.Ordinal);
        Assert.Contains("Reach S2", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Reach S0", output, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_AllTransitions_ContainsEdges()
    {
        var sm = BuildLinearChain();
        var output = new MermaidExporter().Export(sm, ExportType.AllTransitions);
        Assert.Contains("-->", output, StringComparison.Ordinal);
        Assert.Contains("step1", output, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_Default_OutputUnchanged()
    {
        var sm = BuildLinearChain();
        var output = new MermaidExporter().Export(sm, ExportType.Default);
        Assert.Contains("flowchart TD", output, StringComparison.Ordinal);
        Assert.Contains("_start_", output, StringComparison.Ordinal);
    }

    #endregion

    #region GraphMlExporter traversal output

    [Fact]
    public void GraphMlExporter_AllStates_ContainsGraphml()
    {
        var sm = BuildLinearChain();
        var output = new GraphMlExporter().Export(sm, ExportType.AllStates);
        Assert.Contains("graphml", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_AllStates_ContainsMultipleGraphElements()
    {
        // Branch machine produces 2 minimum traversals => 2 graph elements.
        var sm = BuildBranchMachine();
        var output = new GraphMlExporter().Export(sm, ExportType.AllStates);
        var count = CountOccurrences(output, "<graph ");
        Assert.Equal(2, count);
    }

    [Fact]
    public void GraphMlExporter_AllStates_GraphElementsHaveTraversalIds()
    {
        // Branch machine: minimum set is S1 and S2 => IDs T1 and T2, no T3.
        var sm = BuildBranchMachine();
        var output = new GraphMlExporter().Export(sm, ExportType.AllStates);
        Assert.Contains("id=\"T1\"", output, StringComparison.Ordinal);
        Assert.Contains("id=\"T2\"", output, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"T3\"", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_AllTransitions_ContainsEdgeElements()
    {
        var sm = BuildLinearChain();
        var output = new GraphMlExporter().Export(sm, ExportType.AllTransitions);
        Assert.Contains("<edge ", output, StringComparison.Ordinal);
        Assert.Contains("step1", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_Default_OutputUnchanged()
    {
        var sm = BuildLinearChain();
        var output = new GraphMlExporter().Export(sm, ExportType.Default);
        Assert.Contains("graphml", output, StringComparison.Ordinal);
        Assert.Contains("ShapeNode", output, StringComparison.Ordinal);
    }

    #endregion

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
