namespace StateMaker;

public class State
{
    public Dictionary<string, object> Variables { get; } = new();

    public State Clone()
    {
        var clone = new State();
        foreach (var kvp in Variables)
        {
            clone.Variables[kvp.Key] = kvp.Value;
        }
        return clone;
    }
}
