using System.IO;
using StateMaker.Console;
using Xunit;

namespace StateMaker.Tests;

public class EndToEndTests
{
    private static string CreateTempFile(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }

    private static readonly string SampleDefinition = @"{
        ""initialState"": { ""step"": 0, ""done"": false },
        ""rules"": [
            {
                ""name"": ""Advance"",
                ""condition"": ""step < 3"",
                ""transformations"": { ""step"": ""step + 1"" }
            },
            {
                ""name"": ""Finish"",
                ""condition"": ""step == 3 && done == false"",
                ""transformations"": { ""done"": ""true"" }
            }
        ],
        ""config"": { ""maxStates"": 20, ""explorationStrategy"": ""BreadthFirstSearch"" }
    }";

    #region 8.2 — Build with default format (JSON)

    [Fact]
    public void Build_DefaultFormat_OutputsValidJson()
    {
        var path = CreateTempFile(SampleDefinition);
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "build", path }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr.ToString());

            var output = stdout.ToString();
            var importer = new JsonImporter();
            var sm = importer.Import(output);

            // step=0, step=1, step=2, step=3, step=3+done=true = 5 states
            Assert.Equal(5, sm.States.Count);
            Assert.True(sm.IsValidMachine());
            Assert.NotNull(sm.StartingStateId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region 8.3 — Build with --format dot and --format graphml

    [Fact]
    public void Build_DotFormat_OutputsValidDot()
    {
        var path = CreateTempFile(SampleDefinition);
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "build", path, "--format", "dot" }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr.ToString());

            var output = stdout.ToString();
            Assert.Contains("digraph", output, StringComparison.Ordinal);
            Assert.Contains("Advance", output, StringComparison.Ordinal);
            Assert.Contains("Finish", output, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Build_GraphmlFormat_OutputsValidGraphml()
    {
        var path = CreateTempFile(SampleDefinition);
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "build", path, "--format", "graphml" }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr.ToString());

            var output = stdout.ToString();
            Assert.Contains("graphml", output, StringComparison.Ordinal);
            Assert.Contains("node", output, StringComparison.Ordinal);
            Assert.Contains("edge", output, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region 8.4 — Build with --output flag

    [Fact]
    public void Build_WithOutputFlag_CreatesFile()
    {
        var definitionPath = CreateTempFile(SampleDefinition);
        var outputPath = Path.GetTempFileName();
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(
                new[] { "build", definitionPath, "--output", outputPath }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr.ToString());

            var content = File.ReadAllText(outputPath);
            Assert.NotEmpty(content);

            var importer = new JsonImporter();
            var sm = importer.Import(content);
            Assert.Equal(5, sm.States.Count);
        }
        finally
        {
            File.Delete(definitionPath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public void Build_WithOutputAndFormat_CreatesFileInFormat()
    {
        var definitionPath = CreateTempFile(SampleDefinition);
        var outputPath = Path.GetTempFileName();
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(
                new[] { "build", definitionPath, "-o", outputPath, "-f", "dot" }, stdout, stderr);

            Assert.Equal(0, exitCode);

            var content = File.ReadAllText(outputPath);
            Assert.Contains("digraph", content, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(definitionPath);
            File.Delete(outputPath);
        }
    }

    #endregion

    #region 8.5 — Export command

    [Fact]
    public void Export_JsonToDot_ProducesValidDot()
    {
        // First build a state machine JSON
        var definitionPath = CreateTempFile(SampleDefinition);
        var jsonOutputPath = Path.GetTempFileName();
        try
        {
            Program.Run(
                new[] { "build", definitionPath, "-o", jsonOutputPath },
                TextWriter.Null, TextWriter.Null);

            // Now export the JSON to DOT
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(
                new[] { "export", jsonOutputPath, "--format", "dot" }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr.ToString());

            var output = stdout.ToString();
            Assert.Contains("digraph", output, StringComparison.Ordinal);
            Assert.Contains("Advance", output, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(definitionPath);
            File.Delete(jsonOutputPath);
        }
    }

    [Fact]
    public void Export_JsonToGraphml_ProducesValidGraphml()
    {
        var definitionPath = CreateTempFile(SampleDefinition);
        var jsonOutputPath = Path.GetTempFileName();
        try
        {
            Program.Run(
                new[] { "build", definitionPath, "-o", jsonOutputPath },
                TextWriter.Null, TextWriter.Null);

            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(
                new[] { "export", jsonOutputPath, "-f", "graphml" }, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Empty(stderr.ToString());

            var output = stdout.ToString();
            Assert.Contains("graphml", output, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(definitionPath);
            File.Delete(jsonOutputPath);
        }
    }

    [Fact]
    public void Export_WithOutputFlag_CreatesFile()
    {
        var definitionPath = CreateTempFile(SampleDefinition);
        var jsonOutputPath = Path.GetTempFileName();
        var dotOutputPath = Path.GetTempFileName();
        try
        {
            Program.Run(
                new[] { "build", definitionPath, "-o", jsonOutputPath },
                TextWriter.Null, TextWriter.Null);

            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(
                new[] { "export", jsonOutputPath, "-f", "dot", "-o", dotOutputPath }, stdout, stderr);

            Assert.Equal(0, exitCode);

            var content = File.ReadAllText(dotOutputPath);
            Assert.Contains("digraph", content, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(definitionPath);
            File.Delete(jsonOutputPath);
            File.Delete(dotOutputPath);
        }
    }

    #endregion

    #region 8.6 — No arguments shows help

    [Fact]
    public void NoArguments_DisplaysHelp()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = Program.Run(Array.Empty<string>(), stdout, stderr);

        Assert.Equal(0, exitCode);
        Assert.Empty(stderr.ToString());

        var output = stdout.ToString();
        Assert.Contains("Usage:", output, StringComparison.Ordinal);
        Assert.Contains("build", output, StringComparison.Ordinal);
        Assert.Contains("export", output, StringComparison.Ordinal);
    }

    #endregion

    #region 8.7 — Error cases

    [Fact]
    public void Build_MissingFile_ReturnsErrorWithMessage()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = Program.Run(new[] { "build", "does-not-exist.json" }, stdout, stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("not found", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_InvalidJson_ReturnsError()
    {
        var path = CreateTempFile("{ this is not valid json }");
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

    [Fact]
    public void Build_BadFormat_ReturnsError()
    {
        var path = CreateTempFile(SampleDefinition);
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exitCode = Program.Run(new[] { "build", path, "--format", "xml" }, stdout, stderr);

            Assert.Equal(1, exitCode);
            Assert.Contains("xml", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Export_MissingFile_ReturnsError()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = Program.Run(new[] { "export", "missing.json", "-f", "dot" }, stdout, stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("not found", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownCommand_ReturnsError()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        int exitCode = Program.Run(new[] { "destroy" }, stdout, stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("Unknown command", stderr.ToString(), StringComparison.Ordinal);
    }

    #endregion
}
