namespace StateMaker;

public class FilterRule
{
    public string Condition { get; set; } = string.Empty;
    public Dictionary<string, object?> Attributes { get; set; } = new();
}

public class FilterDefinition
{
    public List<FilterRule> Filters { get; set; } = new();
}
