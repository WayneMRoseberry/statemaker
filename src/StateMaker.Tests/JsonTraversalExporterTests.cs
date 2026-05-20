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
    public void AllStates_JsonOutput_TraversalCountIsMinimum()
    {
        // Branch machine: S0->S1 and S0->S2. Both S1 and S2 are terminal so both are kept;
        // S0 is a prefix of both and is filtered out. Result: 2 traversals.
        var sm = new StateMachine();
        sm.AddOrUpdateState("S0", new State());
        sm.AddOrUpdateState("S1", new State());
        sm.AddOrUpdateState("S2", new State());
        sm.StartingStateId = "S0";
        sm.Transitions.Add(new Transition("S0", "S1", "a"));
        sm.Transitions.Add(new Transition("S0", "S2", "b"));

        var json = new JsonExporter().Export(sm, ExportType.AllStates);
        var doc = JsonDocument.Parse(json);
        var traversals = doc.RootElement.GetProperty("traversals");
        Assert.Equal(2, traversals.GetArrayLength());
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
    public void AllTransitions_JsonOutput_TargetTransitionHasCorrectFields()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllTransitions);
        var doc = JsonDocument.Parse(json);
        var traversals = doc.RootElement.GetProperty("traversals");

        // Only traversal is S0->S1->S2; the target (last) transition is S1->S2 via step2.
        var traversal = traversals[0];
        var transitions = traversal.GetProperty("transitions");
        Assert.Equal(2, transitions.GetArrayLength());
        var last = transitions[1];
        Assert.Equal("S1", last.GetProperty("sourceStateId").GetString());
        Assert.Equal("S2", last.GetProperty("targetStateId").GetString());
        Assert.Equal("step2", last.GetProperty("ruleName").GetString());
    }

    [Fact]
    public void AllTransitions_JsonOutput_CountIsMinimum()
    {
        // Linear chain: S0->S1 is a prefix of S0->S1->S2, so only one traversal is emitted.
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllTransitions);
        var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("traversals").GetArrayLength());
    }

    [Fact]
    public void AllTransitions_JsonOutput_SetupPathLeadsToTargetTransitionSource()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllTransitions);
        var doc = JsonDocument.Parse(json);
        var traversal = doc.RootElement.GetProperty("traversals")[0];
        var transitions = traversal.GetProperty("transitions");
        // Setup step S0->S1 appears before the target S1->S2.
        Assert.Equal(2, transitions.GetArrayLength());
        Assert.Equal("S0", transitions[0].GetProperty("sourceStateId").GetString());
        Assert.Equal("S1", transitions[0].GetProperty("targetStateId").GetString());
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

    #region Name includes step count prefix

    [Fact]
    public void AllStates_JsonOutput_NameStartsWithStepCount()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllStates);
        var doc = JsonDocument.Parse(json);
        foreach (var t in doc.RootElement.GetProperty("traversals").EnumerateArray())
        {
            var name = t.GetProperty("name").GetString()!;
            var stepCount = t.GetProperty("transitions").GetArrayLength();
            Assert.StartsWith($"{stepCount} steps - ", name, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AllTransitions_JsonOutput_NameStepCountMatchesTransitionCount()
    {
        var sm = BuildLinearChain();
        var json = new JsonExporter().Export(sm, ExportType.AllTransitions);
        var doc = JsonDocument.Parse(json);
        foreach (var t in doc.RootElement.GetProperty("traversals").EnumerateArray())
        {
            var name = t.GetProperty("name").GetString()!;
            var stepCount = t.GetProperty("transitions").GetArrayLength();
            Assert.StartsWith($"{stepCount} steps - ", name, StringComparison.Ordinal);
        }
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
