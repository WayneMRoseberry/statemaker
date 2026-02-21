using System.Text.Json;
using System.Xml;

namespace StateMaker.Tests;

public class ExporterTests
{
    private static StateMachine BuildSimpleMachine()
    {
        var machine = new StateMachine();
        var s0 = new State();
        s0.Variables["Status"] = "Pending";
        s0.Variables["Count"] = 0;
        var s1 = new State();
        s1.Variables["Status"] = "Approved";
        s1.Variables["Count"] = 1;

        machine.AddOrUpdateState("S0", s0);
        machine.AddOrUpdateState("S1", s1);
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "Approve"));
        return machine;
    }

    private static StateMachine BuildCycleMachine()
    {
        var machine = new StateMachine();
        var s0 = new State();
        s0.Variables["step"] = 0;
        var s1 = new State();
        s1.Variables["step"] = 1;

        machine.AddOrUpdateState("S0", s0);
        machine.AddOrUpdateState("S1", s1);
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "Inc"));
        machine.Transitions.Add(new Transition("S1", "S0", "Reset"));
        return machine;
    }

    private static StateMachine BuildMachineWithAllTypes()
    {
        var machine = new StateMachine();
        var s0 = new State();
        s0.Variables["Name"] = "Test";
        s0.Variables["Count"] = 42;
        s0.Variables["Price"] = 9.99;
        s0.Variables["Active"] = true;

        machine.AddOrUpdateState("S0", s0);
        machine.StartingStateId = "S0";
        return machine;
    }

    private static StateMachine BuildFromBuilder()
    {
        var rule = RuleBuilder.DefineRule("Increment", "[step] < 3",
            new Dictionary<string, string> { { "step", "[step] + 1" } });
        var initialState = new State();
        initialState.Variables["step"] = 0;
        var builder = new StateMachineBuilder();
        return builder.Build(initialState, new IRule[] { rule }, new BuilderConfig { MaxStates = 10 });
    }

    #region 7.3 — JsonExporter

    [Fact]
    public void JsonExporter_ExportsStartingStateId()
    {
        var machine = BuildSimpleMachine();
        var json = new JsonExporter().Export(machine);
        var doc = JsonDocument.Parse(json);
        Assert.Equal("S0", doc.RootElement.GetProperty("startingStateId").GetString());
    }

    [Fact]
    public void JsonExporter_ExportsAllStates()
    {
        var machine = BuildSimpleMachine();
        var json = new JsonExporter().Export(machine);
        var doc = JsonDocument.Parse(json);
        var states = doc.RootElement.GetProperty("states");
        Assert.True(states.TryGetProperty("S0", out _));
        Assert.True(states.TryGetProperty("S1", out _));
    }

    [Fact]
    public void JsonExporter_ExportsStateVariables()
    {
        var machine = BuildSimpleMachine();
        var json = new JsonExporter().Export(machine);
        var doc = JsonDocument.Parse(json);
        var s0 = doc.RootElement.GetProperty("states").GetProperty("S0");
        Assert.Equal("Pending", s0.GetProperty("Status").GetString());
        Assert.Equal(0, s0.GetProperty("Count").GetInt32());
    }

    [Fact]
    public void JsonExporter_ExportsTransitions()
    {
        var machine = BuildSimpleMachine();
        var json = new JsonExporter().Export(machine);
        var doc = JsonDocument.Parse(json);
        var transitions = doc.RootElement.GetProperty("transitions");
        var t = transitions[0];
        Assert.Equal("S0", t.GetProperty("sourceStateId").GetString());
        Assert.Equal("S1", t.GetProperty("targetStateId").GetString());
        Assert.Equal("Approve", t.GetProperty("ruleName").GetString());
    }

    [Fact]
    public void JsonExporter_ExportsAllVariableTypes()
    {
        var machine = BuildMachineWithAllTypes();
        var json = new JsonExporter().Export(machine);
        var doc = JsonDocument.Parse(json);
        var s0 = doc.RootElement.GetProperty("states").GetProperty("S0");
        Assert.Equal("Test", s0.GetProperty("Name").GetString());
        Assert.Equal(42, s0.GetProperty("Count").GetInt32());
        Assert.Equal(9.99, s0.GetProperty("Price").GetDouble());
        Assert.True(s0.GetProperty("Active").GetBoolean());
    }

    [Fact]
    public void JsonExporter_NullStateMachine_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new JsonExporter().Export(null!));
    }

    #endregion

    #region 7.4 — JsonImporter

    [Fact]
    public void JsonImporter_ImportsStartingStateId()
    {
        var json = @"{
            ""startingStateId"": ""S0"",
            ""states"": { ""S0"": { ""x"": 1 } },
            ""transitions"": []
        }";
        var machine = new JsonImporter().Import(json);
        Assert.Equal("S0", machine.StartingStateId);
    }

    [Fact]
    public void JsonImporter_ImportsStates()
    {
        var json = @"{
            ""startingStateId"": ""S0"",
            ""states"": { ""S0"": { ""x"": 1 }, ""S1"": { ""x"": 2 } },
            ""transitions"": []
        }";
        var machine = new JsonImporter().Import(json);
        Assert.Equal(2, machine.States.Count);
        Assert.Equal(1, (int)machine.States["S0"].Variables["x"]!);
        Assert.Equal(2, (int)machine.States["S1"].Variables["x"]!);
    }

    [Fact]
    public void JsonImporter_ImportsTransitions()
    {
        var json = @"{
            ""startingStateId"": ""S0"",
            ""states"": { ""S0"": {}, ""S1"": {} },
            ""transitions"": [
                { ""sourceStateId"": ""S0"", ""targetStateId"": ""S1"", ""ruleName"": ""Go"" }
            ]
        }";
        var machine = new JsonImporter().Import(json);
        Assert.Single(machine.Transitions);
        Assert.Equal("S0", machine.Transitions[0].SourceStateId);
        Assert.Equal("S1", machine.Transitions[0].TargetStateId);
        Assert.Equal("Go", machine.Transitions[0].RuleName);
    }

    [Fact]
    public void JsonImporter_ImportsAllVariableTypes()
    {
        var json = @"{
            ""startingStateId"": ""S0"",
            ""states"": { ""S0"": { ""Name"": ""Test"", ""Count"": 42, ""Price"": 9.99, ""Active"": true } },
            ""transitions"": []
        }";
        var machine = new JsonImporter().Import(json);
        var vars = machine.States["S0"].Variables;
        Assert.Equal("Test", vars["Name"]);
        Assert.Equal(42, (int)vars["Count"]!);
        Assert.Equal(9.99, (double)vars["Price"]!);
        Assert.Equal(true, vars["Active"]);
    }

    [Fact]
    public void JsonImporter_InvalidJson_Throws()
    {
        Assert.Throws<JsonParseException>(() =>
            new JsonImporter().Import("not json {{{"));
    }

    [Fact]
    public void JsonImporter_MissingStates_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new JsonImporter().Import(@"{ ""startingStateId"": ""S0"" }"));
    }

    [Fact]
    public void JsonImporter_NullContent_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new JsonImporter().Import(null!));
    }

    #endregion

    #region 7.5 — JSON Round-Trip

    [Fact]
    public void JsonRoundTrip_SimpleMachine_PreservesStructure()
    {
        var original = BuildSimpleMachine();
        var json = new JsonExporter().Export(original);
        var imported = new JsonImporter().Import(json);

        Assert.Equal(original.StartingStateId, imported.StartingStateId);
        Assert.Equal(original.States.Count, imported.States.Count);
        Assert.Equal(original.Transitions.Count, imported.Transitions.Count);

        foreach (var kvp in original.States)
        {
            Assert.True(imported.States.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, imported.States[kvp.Key]);
        }

        for (int i = 0; i < original.Transitions.Count; i++)
        {
            Assert.Equal(original.Transitions[i].SourceStateId, imported.Transitions[i].SourceStateId);
            Assert.Equal(original.Transitions[i].TargetStateId, imported.Transitions[i].TargetStateId);
            Assert.Equal(original.Transitions[i].RuleName, imported.Transitions[i].RuleName);
        }
    }

    [Fact]
    public void JsonRoundTrip_CycleMachine_PreservesStructure()
    {
        var original = BuildCycleMachine();
        var json = new JsonExporter().Export(original);
        var imported = new JsonImporter().Import(json);

        Assert.Equal(original.States.Count, imported.States.Count);
        Assert.Equal(original.Transitions.Count, imported.Transitions.Count);
        Assert.True(imported.IsValidMachine());
    }

    [Fact]
    public void JsonRoundTrip_AllVariableTypes_PreservesValues()
    {
        var original = BuildMachineWithAllTypes();
        var json = new JsonExporter().Export(original);
        var imported = new JsonImporter().Import(json);

        var vars = imported.States["S0"].Variables;
        Assert.Equal("Test", vars["Name"]);
        Assert.Equal(42, (int)vars["Count"]!);
        Assert.Equal(9.99, (double)vars["Price"]!);
        Assert.Equal(true, vars["Active"]);
    }

    [Fact]
    public void JsonRoundTrip_BuilderProducedMachine_PreservesStructure()
    {
        var original = BuildFromBuilder();
        var json = new JsonExporter().Export(original);
        var imported = new JsonImporter().Import(json);

        Assert.Equal(original.States.Count, imported.States.Count);
        Assert.Equal(original.Transitions.Count, imported.Transitions.Count);
        Assert.Equal(original.StartingStateId, imported.StartingStateId);
        Assert.True(imported.IsValidMachine());
    }

    #endregion

    #region 7.6-7.7 — DotExporter

    [Fact]
    public void DotExporter_ProducesValidDotSyntax()
    {
        var machine = BuildSimpleMachine();
        var dot = new DotExporter().Export(machine);
        Assert.StartsWith("digraph StateMachine {", dot, StringComparison.Ordinal);
        Assert.Contains("}", dot, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_ContainsAllStates()
    {
        var machine = BuildSimpleMachine();
        var dot = new DotExporter().Export(machine);
        Assert.Contains("\"S0\"", dot, StringComparison.Ordinal);
        Assert.Contains("\"S1\"", dot, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_ContainsAllTransitions()
    {
        var machine = BuildSimpleMachine();
        var dot = new DotExporter().Export(machine);
        Assert.Contains("\"S0\" -> \"S1\"", dot, StringComparison.Ordinal);
        Assert.Contains("Approve", dot, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_ContainsStartingStateArrow()
    {
        var machine = BuildSimpleMachine();
        var dot = new DotExporter().Export(machine);
        Assert.Contains("__start", dot, StringComparison.Ordinal);
        Assert.Contains("__start -> \"S0\"", dot, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_NodeLabelsIncludeVariables()
    {
        var machine = BuildSimpleMachine();
        var dot = new DotExporter().Export(machine);
        Assert.Contains("Status", dot, StringComparison.Ordinal);
        Assert.Contains("Pending", dot, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_CycleMachine_ContainsBackEdge()
    {
        var machine = BuildCycleMachine();
        var dot = new DotExporter().Export(machine);
        Assert.Contains("\"S1\" -> \"S0\"", dot, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_NullStateMachine_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DotExporter().Export(null!));
    }

    #endregion

    #region 7.8-7.9 — GraphMlExporter

    [Fact]
    public void GraphMlExporter_ProducesValidXml()
    {
        var machine = BuildSimpleMachine();
        var graphml = new GraphMlExporter().Export(machine);
        var doc = new XmlDocument();
        doc.LoadXml(graphml); // Would throw if invalid XML
    }

    [Fact]
    public void GraphMlExporter_ContainsGraphmlRoot()
    {
        var machine = BuildSimpleMachine();
        var graphml = new GraphMlExporter().Export(machine);
        Assert.Contains("graphml", graphml, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_ContainsAllNodes()
    {
        var machine = BuildSimpleMachine();
        var graphml = new GraphMlExporter().Export(machine);
        Assert.Contains("id=\"S0\"", graphml, StringComparison.Ordinal);
        Assert.Contains("id=\"S1\"", graphml, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_ContainsAllEdges()
    {
        var machine = BuildSimpleMachine();
        var graphml = new GraphMlExporter().Export(machine);
        Assert.Contains("source=\"S0\"", graphml, StringComparison.Ordinal);
        Assert.Contains("target=\"S1\"", graphml, StringComparison.Ordinal);
        Assert.Contains("Approve", graphml, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_ContainsYEdElements()
    {
        var machine = BuildSimpleMachine();
        var graphml = new GraphMlExporter().Export(machine);
        Assert.Contains("ShapeNode", graphml, StringComparison.Ordinal);
        Assert.Contains("NodeLabel", graphml, StringComparison.Ordinal);
        Assert.Contains("EdgeLabel", graphml, StringComparison.Ordinal);
        Assert.Contains("PolyLineEdge", graphml, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_StartingStateHasGreenFill()
    {
        var machine = BuildSimpleMachine();
        var graphml = new GraphMlExporter().Export(machine);
        Assert.Contains("#CCFFCC", graphml, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_NodeLabelsIncludeVariables()
    {
        var machine = BuildSimpleMachine();
        var graphml = new GraphMlExporter().Export(machine);
        Assert.Contains("Status", graphml, StringComparison.Ordinal);
        Assert.Contains("Pending", graphml, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_ContainsKeyDefinitions()
    {
        var machine = BuildSimpleMachine();
        var graphml = new GraphMlExporter().Export(machine);
        Assert.Contains("yfiles.type", graphml, StringComparison.Ordinal);
        Assert.Contains("nodegraphics", graphml, StringComparison.Ordinal);
        Assert.Contains("edgegraphics", graphml, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_NullStateMachine_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new GraphMlExporter().Export(null!));
    }

    #endregion

    #region 7.10 — Import-Then-Re-Export

    [Fact]
    public void ImportThenReExport_JsonToDot_ContainsAllData()
    {
        var original = BuildSimpleMachine();
        var json = new JsonExporter().Export(original);
        var imported = new JsonImporter().Import(json);
        var dot = new DotExporter().Export(imported);

        Assert.Contains("S0", dot, StringComparison.Ordinal);
        Assert.Contains("S1", dot, StringComparison.Ordinal);
        Assert.Contains("Approve", dot, StringComparison.Ordinal);
        Assert.Contains("__start -> \"S0\"", dot, StringComparison.Ordinal);
    }

    [Fact]
    public void ImportThenReExport_JsonToGraphMl_ContainsAllData()
    {
        var original = BuildSimpleMachine();
        var json = new JsonExporter().Export(original);
        var imported = new JsonImporter().Import(json);
        var graphml = new GraphMlExporter().Export(imported);

        Assert.Contains("S0", graphml, StringComparison.Ordinal);
        Assert.Contains("S1", graphml, StringComparison.Ordinal);
        Assert.Contains("Approve", graphml, StringComparison.Ordinal);
        Assert.Contains("ShapeNode", graphml, StringComparison.Ordinal);
    }

    [Fact]
    public void ImportThenReExport_JsonRoundTripThenDotAndGraphMl_NoDataLoss()
    {
        var original = BuildFromBuilder();
        var json1 = new JsonExporter().Export(original);
        var imported = new JsonImporter().Import(json1);
        var json2 = new JsonExporter().Export(imported);

        // JSON round-trip produces identical output
        Assert.Equal(json1, json2);

        // DOT and GraphML contain all states and transitions
        var dot = new DotExporter().Export(imported);
        var graphml = new GraphMlExporter().Export(imported);

        foreach (var stateId in imported.States.Keys)
        {
            Assert.Contains(stateId, dot, StringComparison.Ordinal);
            Assert.Contains(stateId, graphml, StringComparison.Ordinal);
        }

        foreach (var t in imported.Transitions)
        {
            Assert.Contains(t.RuleName, dot, StringComparison.Ordinal);
            Assert.Contains(t.RuleName, graphml, StringComparison.Ordinal);
        }
    }

    #endregion
}
