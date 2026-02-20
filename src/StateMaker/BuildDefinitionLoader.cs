using System.Text.Json;

namespace StateMaker;

public class BuildDefinitionResult
{
    public State InitialState { get; set; } = new();
    public IRule[] Rules { get; set; } = Array.Empty<IRule>();
    public BuilderConfig Config { get; set; } = new();
}

public class BuildDefinitionLoader
{
    private readonly RuleFileLoader _ruleFileLoader = new(new ExpressionEvaluator());

    public BuildDefinitionResult LoadFromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON syntax: {ex.Message}", ex);
        }

        var root = doc.RootElement;

        var (initialState, rules) = _ruleFileLoader.LoadFromJson(json);

        if (initialState is null)
            throw new InvalidOperationException($"Build definition must contain an '{BuildJsonPropertyNames.InitialState}' object.");

        if (!root.TryGetProperty(BuildJsonPropertyNames.Rules, out _))
            throw new InvalidOperationException($"Build definition must contain a '{BuildJsonPropertyNames.Rules}' array.");

        var config = ParseConfig(root);

        return new BuildDefinitionResult
        {
            InitialState = initialState,
            Rules = rules,
            Config = config
        };
    }

    public BuildDefinitionResult LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Build definition file not found: {filePath}", filePath);

        var json = File.ReadAllText(filePath);
        return LoadFromJson(json);
    }

    private static BuilderConfig ParseConfig(JsonElement root)
    {
        var config = new BuilderConfig();

        if (!root.TryGetProperty(BuildJsonPropertyNames.Config, out var configElement))
            return config;

        if (configElement.TryGetProperty(BuildJsonPropertyNames.MaxStates, out var maxStatesElement))
            config.MaxStates = maxStatesElement.GetInt32();

        if (configElement.TryGetProperty(BuildJsonPropertyNames.MaxDepth, out var maxDepthElement))
            config.MaxDepth = maxDepthElement.GetInt32();

        if (configElement.TryGetProperty(BuildJsonPropertyNames.ExplorationStrategy, out var strategyElement))
        {
            var strategyString = strategyElement.GetString();
            config.ExplorationStrategy = strategyString switch
            {
                BuildJsonPropertyNames.BreadthFirstSearch => StateMaker.ExplorationStrategy.BREADTHFIRSTSEARCH,
                BuildJsonPropertyNames.DepthFirstSearch => StateMaker.ExplorationStrategy.DEPTHFIRSTSEARCH,
                _ => throw new InvalidOperationException(
                    $"Unknown exploration strategy '{strategyString}'. Supported values: '{BuildJsonPropertyNames.BreadthFirstSearch}', '{BuildJsonPropertyNames.DepthFirstSearch}'.")
            };
        }

        return config;
    }
}
