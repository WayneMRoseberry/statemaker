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

    #region MermaidExporter

    [Fact]
    public void MermaidExporter_ProducesValidMermaidSyntax()
    {
        var machine = BuildSimpleMachine();
        var mermaid = new MermaidExporter().Export(machine);
        Assert.StartsWith("flowchart TD", mermaid, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_ContainsAllStates()
    {
        var machine = BuildSimpleMachine();
        var mermaid = new MermaidExporter().Export(machine);
        Assert.Contains("S0", mermaid, StringComparison.Ordinal);
        Assert.Contains("S1", mermaid, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_ContainsAllTransitions()
    {
        var machine = BuildSimpleMachine();
        var mermaid = new MermaidExporter().Export(machine);
        Assert.Contains("S0 -->|Approve| S1", mermaid, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_ContainsStartingStateIndicator()
    {
        var machine = BuildSimpleMachine();
        var mermaid = new MermaidExporter().Export(machine);
        Assert.Contains("_start_", mermaid, StringComparison.Ordinal);
        Assert.Contains("--> S0", mermaid, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_NodeLabelsIncludeVariables()
    {
        var machine = BuildSimpleMachine();
        var mermaid = new MermaidExporter().Export(machine);
        Assert.Contains("Status", mermaid, StringComparison.Ordinal);
        Assert.Contains("Pending", mermaid, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_CycleMachine_ContainsBackEdge()
    {
        var machine = BuildCycleMachine();
        var mermaid = new MermaidExporter().Export(machine);
        Assert.Contains("S1 -->|Reset| S0", mermaid, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_NullStateMachine_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MermaidExporter().Export(null!));
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
    public void ImportThenReExport_JsonToMermaid_ContainsAllData()
    {
        var original = BuildSimpleMachine();
        var json = new JsonExporter().Export(original);
        var imported = new JsonImporter().Import(json);
        var mermaid = new MermaidExporter().Export(imported);

        Assert.Contains("S0", mermaid, StringComparison.Ordinal);
        Assert.Contains("S1", mermaid, StringComparison.Ordinal);
        Assert.Contains("Approve", mermaid, StringComparison.Ordinal);
        Assert.Contains("_start_", mermaid, StringComparison.Ordinal);
        Assert.Contains("--> S0", mermaid, StringComparison.Ordinal);
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

    #region JSON Attributes Round-Trip

    [Fact]
    public void JsonExporter_IncludesAttributesInOutput()
    {
        var machine = BuildSimpleMachine();
        machine.States["S0"].Attributes["ranking"] = "high";
        machine.States["S0"].Attributes["priority"] = 1;

        var json = new JsonExporter().Export(machine);
        var doc = JsonDocument.Parse(json);
        var s0 = doc.RootElement.GetProperty("states").GetProperty("S0");

        Assert.True(s0.TryGetProperty("attributes", out var attrs));
        Assert.Equal("high", attrs.GetProperty("ranking").GetString());
        Assert.Equal(1, attrs.GetProperty("priority").GetInt32());
    }

    [Fact]
    public void JsonExporter_OmitsAttributesWhenEmpty()
    {
        var machine = BuildSimpleMachine();

        var json = new JsonExporter().Export(machine);
        var doc = JsonDocument.Parse(json);
        var s0 = doc.RootElement.GetProperty("states").GetProperty("S0");

        Assert.False(s0.TryGetProperty("attributes", out _));
    }

    [Fact]
    public void JsonImporter_ReadsAttributes()
    {
        var machine = BuildSimpleMachine();
        machine.States["S0"].Attributes["ranking"] = "high";
        machine.States["S0"].Attributes["flagged"] = true;

        var json = new JsonExporter().Export(machine);
        var imported = new JsonImporter().Import(json);

        Assert.Equal(2, imported.States["S0"].Attributes.Count);
        Assert.Equal("high", imported.States["S0"].Attributes["ranking"]);
        Assert.True((bool)imported.States["S0"].Attributes["flagged"]!);
    }

    [Fact]
    public void JsonImporter_MissingAttributes_DefaultsToEmpty()
    {
        var machine = BuildSimpleMachine();
        var json = new JsonExporter().Export(machine);
        var imported = new JsonImporter().Import(json);

        Assert.Empty(imported.States["S0"].Attributes);
        Assert.Empty(imported.States["S1"].Attributes);
    }

    [Fact]
    public void JsonRoundTrip_WithAttributes_PreservesAll()
    {
        var machine = BuildSimpleMachine();
        machine.States["S0"].Attributes["ranking"] = "high";
        machine.States["S0"].Attributes["priority"] = 1;
        machine.States["S1"].Attributes["ranking"] = "low";

        var json1 = new JsonExporter().Export(machine);
        var imported = new JsonImporter().Import(json1);
        var json2 = new JsonExporter().Export(imported);

        Assert.Equal(json1, json2);
    }

    #endregion

    #region Attribute Rendering — Helpers

    private static StateMachine BuildMachineWithAttributes()
    {
        var machine = new StateMachine();
        var s0 = new State();
        s0.Variables["Status"] = "Pending";
        s0.Attributes["ranking"] = "high";
        s0.Attributes["flagged"] = true;
        var s1 = new State();
        s1.Variables["Status"] = "Approved";
        // S1 has no attributes

        machine.AddOrUpdateState("S0", s0);
        machine.AddOrUpdateState("S1", s1);
        machine.StartingStateId = "S0";
        machine.Transitions.Add(new Transition("S0", "S1", "Approve"));
        return machine;
    }

    #endregion

    #region Attribute Rendering — DotExporter

    [Fact]
    public void DotExporter_WithAttributes_ContainsAttributesInLabel()
    {
        var machine = BuildMachineWithAttributes();
        var dot = new DotExporter().Export(machine);
        Assert.Contains("ranking", dot, StringComparison.Ordinal);
        Assert.Contains("high", dot, StringComparison.Ordinal);
        Assert.Contains("flagged", dot, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_WithAttributes_ContainsDivider()
    {
        var machine = BuildMachineWithAttributes();
        var dot = new DotExporter().Export(machine);
        // S0 has both variables and attributes, so there should be a visual divider
        Assert.Contains("---", dot, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_WithoutAttributes_NoDivider()
    {
        var machine = BuildMachineWithAttributes();
        var dot = new DotExporter().Export(machine);
        // S1 has no attributes — its label should NOT contain the divider
        // Extract S1's node line and verify no divider
        var lines = dot.Split('\n');
        var s1Line = lines.First(l => l.TrimStart().StartsWith("\"S1\"", StringComparison.Ordinal));
        Assert.DoesNotContain("---", s1Line, StringComparison.Ordinal);
    }

    [Fact]
    public void DotExporter_AttributesSeparatedFromVariables()
    {
        var machine = BuildMachineWithAttributes();
        var dot = new DotExporter().Export(machine);
        var lines = dot.Split('\n');
        var s0Line = lines.First(l => l.TrimStart().StartsWith("\"S0\"", StringComparison.Ordinal));
        // Variables appear before the divider, attributes after
        int statusPos = s0Line.IndexOf("Status", StringComparison.Ordinal);
        int dividerPos = s0Line.IndexOf("---", StringComparison.Ordinal);
        int rankingPos = s0Line.IndexOf("ranking", StringComparison.Ordinal);
        Assert.True(statusPos < dividerPos, "Variables should appear before divider");
        Assert.True(dividerPos < rankingPos, "Attributes should appear after divider");
    }

    #endregion

    #region Attribute Rendering — MermaidExporter

    [Fact]
    public void MermaidExporter_WithAttributes_ContainsAttributesInLabel()
    {
        var machine = BuildMachineWithAttributes();
        var mermaid = new MermaidExporter().Export(machine);
        Assert.Contains("ranking", mermaid, StringComparison.Ordinal);
        Assert.Contains("high", mermaid, StringComparison.Ordinal);
        Assert.Contains("flagged", mermaid, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_WithAttributes_ContainsDivider()
    {
        var machine = BuildMachineWithAttributes();
        var mermaid = new MermaidExporter().Export(machine);
        Assert.Contains("---", mermaid, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_WithoutAttributes_NoDivider()
    {
        var machine = BuildMachineWithAttributes();
        var mermaid = new MermaidExporter().Export(machine);
        var lines = mermaid.Split('\n');
        var s1Line = lines.First(l => l.TrimStart().StartsWith("S1[", StringComparison.Ordinal));
        Assert.DoesNotContain("---", s1Line, StringComparison.Ordinal);
    }

    [Fact]
    public void MermaidExporter_AttributesSeparatedFromVariables()
    {
        var machine = BuildMachineWithAttributes();
        var mermaid = new MermaidExporter().Export(machine);
        var lines = mermaid.Split('\n');
        var s0Line = lines.First(l => l.TrimStart().StartsWith("S0[", StringComparison.Ordinal));
        int statusPos = s0Line.IndexOf("Status", StringComparison.Ordinal);
        int dividerPos = s0Line.IndexOf("---", StringComparison.Ordinal);
        int rankingPos = s0Line.IndexOf("ranking", StringComparison.Ordinal);
        Assert.True(statusPos < dividerPos, "Variables should appear before divider");
        Assert.True(dividerPos < rankingPos, "Attributes should appear after divider");
    }

    #endregion

    #region Attribute Rendering — GraphMlExporter

    [Fact]
    public void GraphMlExporter_WithAttributes_ContainsAttributesInLabel()
    {
        var machine = BuildMachineWithAttributes();
        var graphml = new GraphMlExporter().Export(machine);
        Assert.Contains("ranking", graphml, StringComparison.Ordinal);
        Assert.Contains("high", graphml, StringComparison.Ordinal);
        Assert.Contains("flagged", graphml, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_WithAttributes_ContainsDivider()
    {
        var machine = BuildMachineWithAttributes();
        var graphml = new GraphMlExporter().Export(machine);
        Assert.Contains("---", graphml, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_WithoutAttributes_NoDivider()
    {
        var machine = BuildMachineWithAttributes();
        var graphml = new GraphMlExporter().Export(machine);
        // The GraphML label for S1 should not contain a divider
        // S1's label is in a NodeLabel element — extract and check
        var s1NodeStart = graphml.IndexOf("id=\"S1\"", StringComparison.Ordinal);
        var s1NodeEnd = graphml.IndexOf("</node>", s1NodeStart, StringComparison.Ordinal);
        var s1Section = graphml.Substring(s1NodeStart, s1NodeEnd - s1NodeStart);
        Assert.DoesNotContain("---", s1Section, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphMlExporter_AttributesSeparatedFromVariables()
    {
        var machine = BuildMachineWithAttributes();
        var graphml = new GraphMlExporter().Export(machine);
        // Extract S0 node section
        var s0NodeStart = graphml.IndexOf("id=\"S0\"", StringComparison.Ordinal);
        var s0NodeEnd = graphml.IndexOf("</node>", s0NodeStart, StringComparison.Ordinal);
        var s0Section = graphml.Substring(s0NodeStart, s0NodeEnd - s0NodeStart);
        int statusPos = s0Section.IndexOf("Status", StringComparison.Ordinal);
        int dividerPos = s0Section.IndexOf("---", StringComparison.Ordinal);
        int rankingPos = s0Section.IndexOf("ranking", StringComparison.Ordinal);
        Assert.True(statusPos < dividerPos, "Variables should appear before divider");
        Assert.True(dividerPos < rankingPos, "Attributes should appear after divider");
    }

    #endregion
}
