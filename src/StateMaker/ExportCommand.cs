namespace StateMaker;

public class ExportCommand
{
    private readonly JsonImporter _importer = new();

    public void Execute(string inputFilePath, string? outputPath, string format, TextWriter writer)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"State machine file not found: {inputFilePath}", inputFilePath);

        var json = File.ReadAllText(inputFilePath);
        var stateMachine = _importer.Import(json);

        var exporter = ExporterFactory.GetExporter(format);
        var output = exporter.Export(stateMachine);

        if (outputPath is not null)
            File.WriteAllText(outputPath, output);
        else
            writer.Write(output);
    }
}
