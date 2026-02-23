namespace StateMaker;

public class FilterCommand
{
    private readonly JsonImporter _importer = new();

    public void Execute(string stateMachineFilePath, string filterFilePath, string? outputPath, string format, TextWriter writer)
    {
        if (!File.Exists(stateMachineFilePath))
            throw new FileNotFoundException($"State machine file not found: {stateMachineFilePath}", stateMachineFilePath);

        if (!File.Exists(filterFilePath))
            throw new FileNotFoundException($"Filter definition file not found: {filterFilePath}", filterFilePath);

        var smJson = File.ReadAllText(stateMachineFilePath);
        var stateMachine = _importer.Import(smJson);

        var filterDefinition = FilterDefinitionLoader.LoadFromFile(filterFilePath);

        var filterEngine = new FilterEngine(new ExpressionEvaluator());
        var filterResult = filterEngine.Apply(stateMachine, filterDefinition);

        var pathFilter = new PathFilter(filterResult.StateMachine, filterResult.SelectedStateIds);
        var filteredMachine = pathFilter.Filter();

        var exporter = ExporterFactory.GetExporter(format);
        var output = exporter.Export(filteredMachine);

        if (outputPath is not null)
            File.WriteAllText(outputPath, output);
        else
            writer.Write(output);
    }
}
