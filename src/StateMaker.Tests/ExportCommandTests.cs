using System.IO;
using Xunit;

namespace StateMaker.Tests;

public class ExportCommandTests
{
    private static string CreateTempStateMachineFile()
    {
        var sm = new StateMachine();
        var s0 = new State();
        s0.Variables["x"] = 0;
        var s1 = new State();
        s1.Variables["x"] = 1;
        sm.AddOrUpdateState("S0", s0);
        sm.AddOrUpdateState("S1", s1);
        sm.StartingStateId = "S0";
        sm.Transitions.Add(new Transition("S0", "S1", "Inc"));

        var exporter = new JsonExporter();
        var json = exporter.Export(sm);
        var path = Path.GetTempFileName();
        File.WriteAllText(path, json);
        return path;
    }

    #region Export to stdout

    [Fact]
    public void Execute_JsonFormat_WritesJsonToStdout()
    {
        var path = CreateTempStateMachineFile();
        try
        {
            var writer = new StringWriter();
            var command = new ExportCommand();

            command.Execute(path, null, "json", writer);

            var output = writer.ToString();
            Assert.Contains("startingStateId", output, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Execute_DotFormat_WritesDotToStdout()
    {
        var path = CreateTempStateMachineFile();
        try
        {
            var writer = new StringWriter();
            var command = new ExportCommand();

            command.Execute(path, null, "dot", writer);

            var output = writer.ToString();
            Assert.Contains("digraph", output, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Execute_GraphmlFormat_WritesGraphmlToStdout()
    {
        var path = CreateTempStateMachineFile();
        try
        {
            var writer = new StringWriter();
            var command = new ExportCommand();

            command.Execute(path, null, "graphml", writer);

            var output = writer.ToString();
            Assert.Contains("graphml", output, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region Export to file

    [Fact]
    public void Execute_WithOutputPath_WritesToFile()
    {
        var inputPath = CreateTempStateMachineFile();
        var outputPath = Path.GetTempFileName();
        try
        {
            var command = new ExportCommand();

            command.Execute(inputPath, outputPath, "dot", TextWriter.Null);

            var content = File.ReadAllText(outputPath);
            Assert.Contains("digraph", content, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(inputPath);
            File.Delete(outputPath);
        }
    }

    #endregion

    #region Error cases

    [Fact]
    public void Execute_FileNotFound_ThrowsFileNotFoundException()
    {
        var command = new ExportCommand();

        Assert.Throws<FileNotFoundException>(() =>
            command.Execute("nonexistent.json", null, "json", TextWriter.Null));
    }

    [Fact]
    public void Execute_InvalidFormat_ThrowsArgumentException()
    {
        var path = CreateTempStateMachineFile();
        try
        {
            var command = new ExportCommand();

            Assert.Throws<ArgumentException>(() =>
                command.Execute(path, null, "xml", TextWriter.Null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region Round-trip correctness

    [Fact]
    public void Execute_JsonRoundTrip_PreservesStateMachine()
    {
        var path = CreateTempStateMachineFile();
        try
        {
            var writer = new StringWriter();
            var command = new ExportCommand();

            command.Execute(path, null, "json", writer);

            var importer = new JsonImporter();
            var sm = importer.Import(writer.ToString());
            Assert.Equal(2, sm.States.Count);
            Assert.Single(sm.Transitions);
            Assert.Equal("S0", sm.StartingStateId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region Export with --filter

    private static string CreateTempFilterFile(string filterJson)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, filterJson);
        return path;
    }

    [Fact]
    public void Execute_WithFilter_AppliesFilterAndPathTraversal()
    {
        // Create a 3-state chain: S0 -> S1 -> S2
        var sm = new StateMachine();
        var s0 = new State();
        s0.Variables["x"] = 0;
        var s1 = new State();
        s1.Variables["x"] = 1;
        var s2 = new State();
        s2.Variables["x"] = 2;
        sm.AddOrUpdateState("S0", s0);
        sm.AddOrUpdateState("S1", s1);
        sm.AddOrUpdateState("S2", s2);
        sm.StartingStateId = "S0";
        sm.Transitions.Add(new Transition("S0", "S1", "Step1"));
        sm.Transitions.Add(new Transition("S1", "S2", "Step2"));

        var smPath = Path.GetTempFileName();
        File.WriteAllText(smPath, new JsonExporter().Export(sm));
        var filterPath = CreateTempFilterFile(@"{
            ""filters"": [
                { ""condition"": ""x == 2"", ""attributes"": { ""selected"": true } }
            ]
        }");
        try
        {
            var writer = new StringWriter();
            var command = new ExportCommand();

            command.Execute(smPath, null, "json", writer, filterPath);

            var output = writer.ToString();
            var doc = System.Text.Json.JsonDocument.Parse(output);
            var states = doc.RootElement.GetProperty("states");
            // All states on path from S0 to S2
            Assert.True(states.TryGetProperty("S0", out _));
            Assert.True(states.TryGetProperty("S1", out _));
            Assert.True(states.TryGetProperty("S2", out _));
        }
        finally
        {
            File.Delete(smPath);
            File.Delete(filterPath);
        }
    }

    [Fact]
    public void Execute_WithFilter_NullFilterPath_ExportsWithoutFiltering()
    {
        var path = CreateTempStateMachineFile();
        try
        {
            var writer = new StringWriter();
            var command = new ExportCommand();

            command.Execute(path, null, "json", writer, null);

            var output = writer.ToString();
            Assert.Contains("startingStateId", output, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion
}
