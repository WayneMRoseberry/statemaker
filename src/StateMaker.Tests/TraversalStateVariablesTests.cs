using System.Text.Json;
using Xunit;

namespace StateMaker.Tests;

public class TraversalStateVariablesTests
{
    // S0(x=0) --(inc)--> S1(x=1)
    private static StateMachine BuildMachineWithVariables()
    {
        var sm = new StateMachine();
        var s0 = new State();
        s0.Variables["x"] = 0;
        var s1 = new State();
        s1.Variables["x"] = 1;
        sm.AddOrUpdateState("S0", s0);
        sm.AddOrUpdateState("S1", s1);
        sm.StartingStateId = "S0";
        sm.Transitions.Add(new Transition("S0", "S1", "inc"));
        return sm;
    }

    #region Default: no state variables included

    [Fact]
    public void DotExporter_AllStates_WithoutFlag_DoesNotIncludeVariables()
    {
        var sm = BuildMachineWithVariables();
        var output = new DotExporter().Export(sm, ExportType.AllStates, includeStateVariables: false);
        // Node labels should just be the state ID, not "x=0"
        Assert.DoesNotContain("x=0", output, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_AllStates_WithoutFlag_DoesNotIncludeVariables()
    {
        var sm = BuildMachineWithVariables();
        var output = new MermaidExporter().Export(sm, ExportType.AllStates, includeStateVariables: false);
        Assert.DoesNotContain("x=0", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_AllStates_WithoutFlag_DoesNotIncludeVariables()
    {
        var sm = BuildMachineWithVariables();
        var output = new GraphMlExporter().Export(sm, ExportType.AllStates, includeStateVariables: false);
        Assert.DoesNotContain("x=0", output, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonExporter_AllStates_WithoutFlag_DoesNotIncludeStatesSection()
    {
        var sm = BuildMachineWithVariables();
        var json = new JsonExporter().Export(sm, ExportType.AllStates, includeStateVariables: false);
        var doc = JsonDocument.Parse(json);
        var traversals = doc.RootElement.GetProperty("traversals");
        foreach (var t in traversals.EnumerateArray())
        {
            Assert.False(t.TryGetProperty("states", out _),
                "Traversal should not have a 'states' section when includeStateVariables is false");
        }
    }

    #endregion

    #region With flag: state variables included

    [Fact]
    public void DotExporter_AllStates_WithFlag_IncludesVariablesInNodeLabels()
    {
        var sm = BuildMachineWithVariables();
        var output = new DotExporter().Export(sm, ExportType.AllStates, includeStateVariables: true);
        Assert.Contains("x=0", output, StringComparison.Ordinal);
        Assert.Contains("x=1", output, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_AllTransitions_WithFlag_IncludesVariables()
    {
        var sm = BuildMachineWithVariables();
        var output = new DotExporter().Export(sm, ExportType.AllTransitions, includeStateVariables: true);
        Assert.Contains("x=0", output, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_AllStates_WithFlag_IncludesVariablesInSubgraphNodes()
    {
        var sm = BuildMachineWithVariables();
        var output = new MermaidExporter().Export(sm, ExportType.AllStates, includeStateVariables: true);
        Assert.Contains("x=0", output, StringComparison.Ordinal);
        Assert.Contains("x=1", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_AllStates_WithFlag_IncludesVariablesInNodeLabels()
    {
        var sm = BuildMachineWithVariables();
        var output = new GraphMlExporter().Export(sm, ExportType.AllStates, includeStateVariables: true);
        Assert.Contains("x=0", output, StringComparison.Ordinal);
        Assert.Contains("x=1", output, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonExporter_AllStates_WithFlag_IncludesStatesSectionPerTraversal()
    {
        var sm = BuildMachineWithVariables();
        var json = new JsonExporter().Export(sm, ExportType.AllStates, includeStateVariables: true);
        var doc = JsonDocument.Parse(json);
        var traversals = doc.RootElement.GetProperty("traversals");
        // Every traversal should have a "states" property
        foreach (var t in traversals.EnumerateArray())
        {
            Assert.True(t.TryGetProperty("states", out _),
                "Each traversal should have a 'states' section when includeStateVariables is true");
        }
    }

    [Fact]
    public void JsonExporter_AllStates_WithFlag_StatesContainVariableValues()
    {
        var sm = BuildMachineWithVariables();
        var json = new JsonExporter().Export(sm, ExportType.AllStates, includeStateVariables: true);
        var doc = JsonDocument.Parse(json);
        // T2 reaches S1, so its states section should include S1 with x=1
        var traversals = doc.RootElement.GetProperty("traversals");
        bool foundS1WithVariable = false;
        foreach (var t in traversals.EnumerateArray())
        {
            if (t.TryGetProperty("states", out var states) &&
                states.TryGetProperty("S1", out var s1) &&
                s1.TryGetProperty("x", out var xProp) &&
                xProp.GetInt32() == 1)
            {
                foundS1WithVariable = true;
            }
        }
        Assert.True(foundS1WithVariable, "Expected a traversal with S1 having x=1 in its states section");
    }

    #endregion

    #region Default export behavior still unchanged

    [Fact]
    public void DotExporter_Default_IgnoresIncludeStateVariablesFlag()
    {
        var sm = BuildMachineWithVariables();
        // Default export always includes variables (existing behavior) regardless of flag
        var output = new DotExporter().Export(sm, ExportType.Default, includeStateVariables: false);
        Assert.Contains("digraph StateMachine", output, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonExporter_Default_IgnoresIncludeStateVariablesFlag()
    {
        var sm = BuildMachineWithVariables();
        var json = new JsonExporter().Export(sm, ExportType.Default, includeStateVariables: false);
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("startingStateId", out _));
    }

    #endregion

    #region CLI flag wiring

    [Fact]
    public void HelpPrinter_ContainsIncludeStateVariablesFlag()
    {
        var writer = new StringWriter();
        HelpPrinter.PrintHelp(writer);
        Assert.Contains("--include-state-variables", writer.ToString(), StringComparison.Ordinal);
    }

    #endregion
}
