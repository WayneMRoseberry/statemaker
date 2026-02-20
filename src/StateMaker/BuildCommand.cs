namespace StateMaker;

public class BuildCommand
{
    private readonly BuildDefinitionLoader _loader = new();

    public void Execute(string definitionFilePath, string? outputPath, string format, TextWriter writer)
    {
        var result = _loader.LoadFromFile(definitionFilePath);
        var builder = new StateMachineBuilder();
        var stateMachine = builder.Build(result.InitialState, result.Rules, result.Config);

        var exporter = ExporterFactory.GetExporter(format);
        var output = exporter.Export(stateMachine);

        if (outputPath is not null)
            File.WriteAllText(outputPath, output);
        else
            writer.Write(output);
    }
}
