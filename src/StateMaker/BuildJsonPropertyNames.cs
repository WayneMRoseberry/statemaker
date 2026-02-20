namespace StateMaker;

internal static class BuildJsonPropertyNames
{
    // Build definition sections
    public const string InitialState = "initialState";
    public const string Rules = "rules";
    public const string Config = "config";

    // Rule properties
    public const string Type = "type";
    public const string Name = "name";
    public const string Condition = "condition";
    public const string Transformations = "transformations";
    public const string AssemblyPath = "assemblyPath";
    public const string ClassName = "className";

    // Rule type values
    public const string Declarative = "declarative";
    public const string Custom = "custom";

    // Config properties
    public const string MaxStates = "maxStates";
    public const string MaxDepth = "maxDepth";
    public const string ExplorationStrategy = "explorationStrategy";

    // Exploration strategy values
    public const string BreadthFirstSearch = "BreadthFirstSearch";
    public const string DepthFirstSearch = "DepthFirstSearch";
}
