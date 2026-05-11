using System.Text.Json;
using Xunit;

namespace StateMaker.Tests;

public class JsonTraversalExporterTests
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

    #region Output structure

    [Fact]
    public void AllStates_JsonOutput_HasTraversalsArray()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllStates);
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("traversals").ValueKind);
    }

    [Fact]
    public void AllStates_JsonOutput_TraversalCountMatchesStateCount()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllStates);
        var doc = JsonDocument.Parse(json);
        var traversals = doc.RootElement.GetProperty("traversals");
        Assert.Equal(3, traversals.GetArrayLength());
    }

    [Fact]
    public void AllStates_JsonOutput_EachTraversalHasIdNameDescription()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllStates);
        var doc = JsonDocument.Parse(json);
        foreach (var t in doc.RootElement.GetProperty("traversals").EnumerateArray())
        {
            Assert.True(t.TryGetProperty("id", out var id));
            Assert.True(t.TryGetProperty("name", out var name));
            Assert.True(t.TryGetProperty("description", out var desc));
            Assert.False(string.IsNullOrWhiteSpace(id.GetString()));
            Assert.False(string.IsNullOrWhiteSpace(name.GetString()));
            Assert.False(string.IsNullOrWhiteSpace(desc.GetString()));
        }
    }

    [Fact]
    public void AllStates_JsonOutput_EachTraversalHasTransitionsArray()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllStates);
        var doc = JsonDocument.Parse(json);
        foreach (var t in doc.RootElement.GetProperty("traversals").EnumerateArray())
        {
            Assert.Equal(JsonValueKind.Array, t.GetProperty("transitions").ValueKind);
        }
    }

    #endregion

    #region Transition fields

    [Fact]
    public void AllTransitions_JsonOutput_TransitionHasCorrectFields()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllTransitions);
        var doc = JsonDocument.Parse(json);
        var traversals = doc.RootElement.GetProperty("traversals");

        // First traversal covers S0->S1 via step1; its last (only) transition should reflect this
        var firstTraversal = traversals[0];
        var transitions = firstTraversal.GetProperty("transitions");
        Assert.Equal(1, transitions.GetArrayLength());
        var tr = transitions[0];
        Assert.Equal("S0", tr.GetProperty("sourceStateId").GetString());
        Assert.Equal("S1", tr.GetProperty("targetStateId").GetString());
        Assert.Equal("step1", tr.GetProperty("ruleName").GetString());
    }

    [Fact]
    public void AllTransitions_JsonOutput_CountMatchesTransitionCount()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllTransitions);
        var doc = JsonDocument.Parse(json);
        Assert.Equal(2, doc.RootElement.GetProperty("traversals").GetArrayLength());
    }

    [Fact]
    public void AllTransitions_JsonOutput_SecondTraversalPathLeadsToTransition()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllTransitions);
        var doc = JsonDocument.Parse(json);
        var secondTraversal = doc.RootElement.GetProperty("traversals")[1];
        var transitions = secondTraversal.GetProperty("transitions");
        // Path to S1->S2 must include S0->S1 first
        Assert.Equal(2, transitions.GetArrayLength());
        Assert.Equal("S1", transitions[1].GetProperty("sourceStateId").GetString());
        Assert.Equal("S2", transitions[1].GetProperty("targetStateId").GetString());
    }

    #endregion

    #region AllPaths and AllStatePairs

    [Fact]
    public void AllPaths_JsonOutput_HasTraversalsArray()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllPaths);
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("traversals").ValueKind);
    }

    [Fact]
    public void AllPaths_LinearChain_ProducesOnePath()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllPaths);
        var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("traversals").GetArrayLength());
    }

    [Fact]
    public void AllStatePairs_JsonOutput_HasTraversalsArray()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllStatePairs);
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("traversals").ValueKind);
    }

    [Fact]
    public void AllStatePairs_LinearChain_ProducesThreePairs()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllStatePairs);
        var doc = JsonDocument.Parse(json);
        Assert.Equal(3, doc.RootElement.GetProperty("traversals").GetArrayLength());
    }

    #endregion

    #region Default behavior unchanged

    [Fact]
    public void Default_JsonOutput_StillProducesStateMachineSchema()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.Default);
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("startingStateId", out _));
        Assert.True(doc.RootElement.TryGetProperty("states", out _));
        Assert.True(doc.RootElement.TryGetProperty("transitions", out _));
    }

    #endregion
}
