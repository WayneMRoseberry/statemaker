using System.IO;
using System.Text.Json;
using Xunit;

namespace StateMaker.Tests;

public class FilterCommandTests
{
    private static string CreateTempStateMachineFile()
    {
        var sm = new StateMachine();
        var s0 = new State();
        s0.Variables["status"] = "start";
        var s1 = new State();
        s1.Variables["status"] = "middle";
        var s2 = new State();
        s2.Variables["status"] = "end";
        sm.AddOrUpdateState("S0", s0);
        sm.AddOrUpdateState("S1", s1);
        sm.AddOrUpdateState("S2", s2);
        sm.StartingStateId = "S0";
        sm.Transitions.Add(new Transition("S0", "S1", "Step1"));
        sm.Transitions.Add(new Transition("S1", "S2", "Step2"));

        var json = new JsonExporter().Export(sm);
        var path = Path.GetTempFileName();
        File.WriteAllText(path, json);
        return path;
    }

    private static string CreateTempFilterFile(string filterJson)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, filterJson);
        return path;
    }

    private static readonly string SimpleFilter = @"{
        ""filters"": [
            {
                ""condition"": ""status == 'end'"",
                ""attributes"": { ""highlight"": ""red"" }
            }
        ]
    }";

    #region Successful filtering

    [Fact]
    public void Execute_WithFilter_OutputsFilteredMachine()
    {
        var smPath = CreateTempStateMachineFile();
        var filterPath = CreateTempFilterFile(SimpleFilter);
        try
        {
            var writer = new StringWriter();
            var command = new FilterCommand();

            command.Execute(smPath, filterPath, null, "json", writer);

            var output = writer.ToString();
            var doc = JsonDocument.Parse(output);
            var states = doc.RootElement.GetProperty("states");
            // Path from S0 to S2 goes through S1, so all three states should be present
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
    public void Execute_WithFilter_AppliesAttributes()
    {
        var smPath = CreateTempStateMachineFile();
        var filterPath = CreateTempFilterFile(SimpleFilter);
        try
        {
            var writer = new StringWriter();
            var command = new FilterCommand();

            command.Execute(smPath, filterPath, null, "json", writer);

            var output = writer.ToString();
            var doc = JsonDocument.Parse(output);
            var s2 = doc.RootElement.GetProperty("states").GetProperty("S2");
            Assert.True(s2.TryGetProperty("attributes", out var attrs));
            Assert.Equal("red", attrs.GetProperty("highlight").GetString());
        }
        finally
        {
            File.Delete(smPath);
            File.Delete(filterPath);
        }
    }

    [Fact]
    public void Execute_DotFormat_WritesDotOutput()
    {
        var smPath = CreateTempStateMachineFile();
        var filterPath = CreateTempFilterFile(SimpleFilter);
        try
        {
            var writer = new StringWriter();
            var command = new FilterCommand();

            command.Execute(smPath, filterPath, null, "dot", writer);

            var output = writer.ToString();
            Assert.Contains("digraph", output, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(smPath);
            File.Delete(filterPath);
        }
    }

    [Fact]
    public void Execute_WithOutputPath_WritesToFile()
    {
        var smPath = CreateTempStateMachineFile();
        var filterPath = CreateTempFilterFile(SimpleFilter);
        var outputPath = Path.GetTempFileName();
        try
        {
            var command = new FilterCommand();

            command.Execute(smPath, filterPath, outputPath, "json", TextWriter.Null);

            var content = File.ReadAllText(outputPath);
            Assert.Contains("startingStateId", content, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(smPath);
            File.Delete(filterPath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public void Execute_NoMatchingStates_OutputsEmptyMachine()
    {
        var smPath = CreateTempStateMachineFile();
        var filterPath = CreateTempFilterFile(@"{
            ""filters"": [
                { ""condition"": ""status == 'nonexistent'"", ""attributes"": {} }
            ]
        }");
        try
        {
            var writer = new StringWriter();
            var command = new FilterCommand();

            command.Execute(smPath, filterPath, null, "json", writer);

            var output = writer.ToString();
            var doc = JsonDocument.Parse(output);
            var states = doc.RootElement.GetProperty("states");
            Assert.Empty(states.EnumerateObject().ToList());
        }
        finally
        {
            File.Delete(smPath);
            File.Delete(filterPath);
        }
    }

    #endregion

    #region Error cases

    [Fact]
    public void Execute_StateMachineFileNotFound_ThrowsFileNotFoundException()
    {
        var filterPath = CreateTempFilterFile(SimpleFilter);
        try
        {
            var command = new FilterCommand();

            Assert.Throws<FileNotFoundException>(() =>
                command.Execute("nonexistent.json", filterPath, null, "json", TextWriter.Null));
        }
        finally
        {
            File.Delete(filterPath);
        }
    }

    [Fact]
    public void Execute_FilterFileNotFound_ThrowsFileNotFoundException()
    {
        var smPath = CreateTempStateMachineFile();
        try
        {
            var command = new FilterCommand();

            Assert.Throws<FileNotFoundException>(() =>
                command.Execute(smPath, "nonexistent-filter.json", null, "json", TextWriter.Null));
        }
        finally
        {
            File.Delete(smPath);
        }
    }

    [Fact]
    public void Execute_InvalidFilterDefinition_Throws()
    {
        var smPath = CreateTempStateMachineFile();
        var filterPath = CreateTempFilterFile("not valid json");
        try
        {
            var command = new FilterCommand();

            Assert.ThrowsAny<Exception>(() =>
                command.Execute(smPath, filterPath, null, "json", TextWriter.Null));
        }
        finally
        {
            File.Delete(smPath);
            File.Delete(filterPath);
        }
    }

    [Fact]
    public void Execute_InvalidFormat_ThrowsArgumentException()
    {
        var smPath = CreateTempStateMachineFile();
        var filterPath = CreateTempFilterFile(SimpleFilter);
        try
        {
            var command = new FilterCommand();

            Assert.Throws<ArgumentException>(() =>
                command.Execute(smPath, filterPath, null, "xml", TextWriter.Null));
        }
        finally
        {
            File.Delete(smPath);
            File.Delete(filterPath);
        }
    }

    #endregion
}
