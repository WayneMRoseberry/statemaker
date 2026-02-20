using System.IO;
using Xunit;

namespace StateMaker.Tests;

public class BuildCommandTests
{
    private static string CreateTempDefinitionFile(string json)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, json);
        return path;
    }

    private static readonly string SimpleDefinition = @"{
        ""initialState"": { ""step"": 0 },
        ""rules"": [
            {
                ""name"": ""Step"",
                ""condition"": ""step < 2"",
                ""transformations"": { ""step"": ""step + 1"" }
            }
        ],
        ""config"": { ""maxStates"": 10 }
    }";

    #region Build to stdout

    [Fact]
    public void Execute_DefaultFormat_WritesJsonToStdout()
    {
        var path = CreateTempDefinitionFile(SimpleDefinition);
        try
        {
            var writer = new StringWriter();
            var command = new BuildCommand();

            command.Execute(path, null, "json", writer);

            var output = writer.ToString();
            Assert.Contains("startingStateId", output, StringComparison.Ordinal);
            Assert.Contains("states", output, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Execute_DotFormat_WritesDotToStdout()
    {
        var path = CreateTempDefinitionFile(SimpleDefinition);
        try
        {
            var writer = new StringWriter();
            var command = new BuildCommand();

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
        var path = CreateTempDefinitionFile(SimpleDefinition);
        try
        {
            var writer = new StringWriter();
            var command = new BuildCommand();

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

    #region Build to file

    [Fact]
    public void Execute_WithOutputPath_WritesToFile()
    {
        var definitionPath = CreateTempDefinitionFile(SimpleDefinition);
        var outputPath = Path.GetTempFileName();
        try
        {
            var command = new BuildCommand();

            command.Execute(definitionPath, outputPath, "json", TextWriter.Null);

            var content = File.ReadAllText(outputPath);
            Assert.Contains("startingStateId", content, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(definitionPath);
            File.Delete(outputPath);
        }
    }

    #endregion

    #region Error cases

    [Fact]
    public void Execute_FileNotFound_ThrowsFileNotFoundException()
    {
        var command = new BuildCommand();

        Assert.Throws<FileNotFoundException>(() =>
            command.Execute("nonexistent.json", null, "json", TextWriter.Null));
    }

    [Fact]
    public void Execute_InvalidFormat_ThrowsArgumentException()
    {
        var path = CreateTempDefinitionFile(SimpleDefinition);
        try
        {
            var command = new BuildCommand();

            Assert.Throws<ArgumentException>(() =>
                command.Execute(path, null, "xml", TextWriter.Null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region State machine correctness

    [Fact]
    public void Execute_SimpleDefinition_ProducesCorrectStateCount()
    {
        var path = CreateTempDefinitionFile(SimpleDefinition);
        try
        {
            var writer = new StringWriter();
            var command = new BuildCommand();

            command.Execute(path, null, "json", writer);

            var output = writer.ToString();
            // step 0, 1, 2 = 3 states
            var importer = new JsonImporter();
            var sm = importer.Import(output);
            Assert.Equal(3, sm.States.Count);
            Assert.Equal(2, sm.Transitions.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion
}
