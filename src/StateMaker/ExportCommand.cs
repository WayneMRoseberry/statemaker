namespace StateMaker;

public class ExportCommand
{
    private readonly JsonImporter _importer = new();

    public void Execute(string inputFilePath, string? outputPath, string format, TextWriter writer)
    {
        Execute(inputFilePath, outputPath, format, writer, null);
    }

    public void Execute(string inputFilePath, string? outputPath, string format, TextWriter writer, string? filterFilePath)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"State machine file not found: {inputFilePath}", inputFilePath);

        var json = File.ReadAllText(inputFilePath);
        var stateMachine = _importer.Import(json);

        if (filterFilePath is not null)
        {
            var filterDefinition = FilterDefinitionLoader.LoadFromFile(filterFilePath);
            var filterEngine = new FilterEngine(new ExpressionEvaluator());
            var filterResult = filterEngine.Apply(stateMachine, filterDefinition);
            var pathFilter = new PathFilter(filterResult.StateMachine, filterResult.SelectedStateIds);
            stateMachine = pathFilter.Filter();
        }

        var exporter = ExporterFactory.GetExporter(format);
        var output = exporter.Export(stateMachine);

        if (outputPath is not null)
            File.WriteAllText(outputPath, output);
        else
            writer.Write(output);
    }
}
