namespace StateMaker;

public class State : IEquatable<State>
{
    public Dictionary<string, object?> Variables { get; } = new();
    public Dictionary<string, object?> Attributes { get; } = new();

    public State Clone()
    {
        var clone = new State();
        foreach (var kvp in Variables)
        {
            clone.Variables[kvp.Key] = kvp.Value;
        }
        foreach (var kvp in Attributes)
        {
            clone.Attributes[kvp.Key] = kvp.Value;
        }
        return clone;
    }

    public bool Equals(State? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Variables.Count != other.Variables.Count)
            return false;

        foreach (var kvp in Variables)
        {
            if (!other.Variables.TryGetValue(kvp.Key, out var otherValue))
                return false;

            if (!Equals(kvp.Value, otherValue))
                return false;
        }

        if (Attributes.Count != other.Attributes.Count)
            return false;

        foreach (var kvp in Attributes)
        {
            if (!other.Attributes.TryGetValue(kvp.Key, out var otherValue))
                return false;

            if (!Equals(kvp.Value, otherValue))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is State other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var kvp in Variables.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            hash.Add(kvp.Key, StringComparer.Ordinal);
            hash.Add(kvp.Value);
        }
        foreach (var kvp in Attributes.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            hash.Add(kvp.Key, StringComparer.Ordinal);
            hash.Add(kvp.Value);
        }
        return hash.ToHashCode();
    }
}
