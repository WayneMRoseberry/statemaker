using System.IO;
using StateMaker.Console;
using Xunit;

namespace StateMaker.Tests;

public class ProgramTests
{
    #region Help / No Arguments

    [Fact]
    public void Run_NoArguments_PrintsHelpAndReturnsZero()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = Program.Run(Array.Empty<string>(), stdout, stderr);

        Assert.Equal(0, exitCode);
        Assert.Contains("Usage:", stdout.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Run_UnknownCommand_PrintsHelpAndReturnsOne()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = Program.Run(new[] { "unknown" }, stdout, stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("Unknown command", stderr.ToString(), StringComparison.Ordinal);
    }

    #endregion

    #region Build Command Routing

    [Fact]
    public void Run_BuildCommand_WithDefinitionFile_ReturnsZero()
    {
        var definitionPath = Path.GetTempFileName();
        File.WriteAllText(definitionPath, @"{
            ""initialState"": { ""x"": 0 },
            ""rules"": [
                { ""name"": ""Inc"", ""condition"": ""x < 1"", ""transformations"": { ""x"": ""x + 1"" } }
            ]
        }");
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "build", definitionPath }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("startingStateId", stdout.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(definitionPath);
        }
    }

    [Fact]
    public void Run_BuildCommand_WithFormatFlag_UsesSpecifiedFormat()
    {
        var definitionPath = Path.GetTempFileName();
        File.WriteAllText(definitionPath, @"{
            ""initialState"": { ""x"": 0 },
            ""rules"": [
                { ""name"": ""Inc"", ""condition"": ""x < 1"", ""transformations"": { ""x"": ""x + 1"" } }
            ]
        }");
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "build", definitionPath, "--format", "dot" }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("digraph", stdout.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(definitionPath);
        }
    }

    [Fact]
    public void Run_BuildCommand_WithShortFormatFlag_UsesSpecifiedFormat()
    {
        var definitionPath = Path.GetTempFileName();
        File.WriteAllText(definitionPath, @"{
            ""initialState"": { ""x"": 0 },
            ""rules"": [
                { ""name"": ""Inc"", ""condition"": ""x < 1"", ""transformations"": { ""x"": ""x + 1"" } }
            ]
        }");
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "build", definitionPath, "-f", "dot" }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("digraph", stdout.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(definitionPath);
        }
    }

    [Fact]
    public void Run_BuildCommand_WithOutputFlag_WritesToFile()
    {
        var definitionPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();
        File.WriteAllText(definitionPath, @"{
            ""initialState"": { ""x"": 0 },
            ""rules"": [
                { ""name"": ""Inc"", ""condition"": ""x < 1"", ""transformations"": { ""x"": ""x + 1"" } }
            ]
        }");
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "build", definitionPath, "--output", outputPath }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("startingStateId", File.ReadAllText(outputPath), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(definitionPath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public void Run_BuildCommand_WithShortOutputFlag_WritesToFile()
    {
        var definitionPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();
        File.WriteAllText(definitionPath, @"{
            ""initialState"": { ""x"": 0 },
            ""rules"": [
                { ""name"": ""Inc"", ""condition"": ""x < 1"", ""transformations"": { ""x"": ""x + 1"" } }
            ]
        }");
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "build", definitionPath, "-o", outputPath }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("startingStateId", File.ReadAllText(outputPath), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(definitionPath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public void Run_BuildCommand_MissingFilePath_ReturnsOne()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = Program.Run(new[] { "build" }, stdout, stderr);

        Assert.Equal(1, exitCode);
        Assert.NotEmpty(stderr.ToString());
    }

    #endregion

    #region Export Command Routing

    [Fact]
    public void Run_ExportCommand_WithJsonFile_ReturnsZero()
    {
        var sm = new StateMachine();
        var s0 = new State();
        s0.Variables["x"] = 0;
        sm.AddOrUpdateState("S0", s0);
        sm.StartingStateId = "S0";
        var json = new JsonExporter().Export(sm);
        var inputPath = Path.GetTempFileName();
        File.WriteAllText(inputPath, json);
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "export", inputPath, "--format", "dot" }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("digraph", stdout.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(inputPath);
        }
    }

    [Fact]
    public void Run_ExportCommand_MissingFilePath_ReturnsOne()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = Program.Run(new[] { "export" }, stdout, stderr);

        Assert.Equal(1, exitCode);
        Assert.NotEmpty(stderr.ToString());
    }

    [Fact]
    public void Run_ExportCommand_MissingFormatFlag_DefaultsToJson()
    {
        var sm = new StateMachine();
        var s0 = new State();
        s0.Variables["x"] = 0;
        sm.AddOrUpdateState("S0", s0);
        sm.StartingStateId = "S0";
        var json = new JsonExporter().Export(sm);
        var inputPath = Path.GetTempFileName();
        File.WriteAllText(inputPath, json);
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "export", inputPath }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("startingStateId", stdout.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(inputPath);
        }
    }

    #endregion

    #region Error Handling

    [Fact]
    public void Run_BuildCommand_FileNotFound_ReturnsOneWithStackTrace()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = Program.Run(new[] { "build", "nonexistent.json" }, stdout, stderr);

        Assert.Equal(1, exitCode);
        var errorOutput = stderr.ToString();
        Assert.Contains("not found", errorOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_BuildCommand_InvalidJson_ReturnsOne()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "not valid json");
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "build", path }, stdout, stderr);

            Assert.Equal(1, exitCode);
            Assert.NotEmpty(stderr.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region Filter Command Routing

    [Fact]
    public void Run_FilterCommand_WithRequiredArgs_ReturnsZero()
    {
        var sm = new StateMachine();
        var s0 = new State();
        s0.Variables["x"] = 0;
        sm.AddOrUpdateState("S0", s0);
        sm.StartingStateId = "S0";
        var smPath = Path.GetTempFileName();
        File.WriteAllText(smPath, new JsonExporter().Export(sm));
        var filterPath = Path.GetTempFileName();
        File.WriteAllText(filterPath, @"{ ""filters"": [ { ""condition"": ""x == 0"", ""attributes"": {} } ] }");
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "filter", smPath, filterPath }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("startingStateId", stdout.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(smPath);
            File.Delete(filterPath);
        }
    }

    [Fact]
    public void Run_FilterCommand_WithFormatFlag_UsesSpecifiedFormat()
    {
        var sm = new StateMachine();
        var s0 = new State();
        s0.Variables["x"] = 0;
        sm.AddOrUpdateState("S0", s0);
        sm.StartingStateId = "S0";
        var smPath = Path.GetTempFileName();
        File.WriteAllText(smPath, new JsonExporter().Export(sm));
        var filterPath = Path.GetTempFileName();
        File.WriteAllText(filterPath, @"{ ""filters"": [ { ""condition"": ""x == 0"", ""attributes"": {} } ] }");
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "filter", smPath, filterPath, "--format", "dot" }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("digraph", stdout.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(smPath);
            File.Delete(filterPath);
        }
    }

    [Fact]
    public void Run_FilterCommand_MissingArgs_ReturnsOne()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = Program.Run(new[] { "filter" }, stdout, stderr);

        Assert.Equal(1, exitCode);
        Assert.NotEmpty(stderr.ToString());
    }

    [Fact]
    public void Run_FilterCommand_MissingFilterFile_ReturnsOne()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = Program.Run(new[] { "filter", "somefile.json" }, stdout, stderr);

        Assert.Equal(1, exitCode);
        Assert.NotEmpty(stderr.ToString());
    }

    [Fact]
    public void Run_FilterCommand_WithListFlag_OutputsJsonArray()
    {
        var sm = new StateMachine();
        var s0 = new State();
        s0.Variables["x"] = 0;
        sm.AddOrUpdateState("S0", s0);
        sm.StartingStateId = "S0";
        var smPath = Path.GetTempFileName();
        File.WriteAllText(smPath, new JsonExporter().Export(sm));
        var filterPath = Path.GetTempFileName();
        File.WriteAllText(filterPath, @"{ ""filters"": [ { ""condition"": ""x == 0"", ""attributes"": { ""matched"": true } } ] }");
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "filter", smPath, filterPath, "--list" }, stdout, stderr);

            Assert.Equal(0, exitCode);
            var output = stdout.ToString();
            Assert.Contains("\"stateId\"", output, StringComparison.Ordinal);
            Assert.Contains("\"variables\"", output, StringComparison.Ordinal);
            Assert.Contains("\"attributes\"", output, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(smPath);
            File.Delete(filterPath);
        }
    }

    [Fact]
    public void Run_ExportCommand_WithFilterFlag_AppliesFilter()
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
        var smPath = Path.GetTempFileName();
        File.WriteAllText(smPath, new JsonExporter().Export(sm));
        var filterPath = Path.GetTempFileName();
        File.WriteAllText(filterPath, @"{ ""filters"": [ { ""condition"": ""x == 1"", ""attributes"": { ""found"": true } } ] }");
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "export", smPath, "--filter", filterPath }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Contains("found", stdout.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(smPath);
            File.Delete(filterPath);
        }
    }

    #endregion
}
