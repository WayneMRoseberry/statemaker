namespace StateMaker;

public class Traversal
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<Transition> Transitions { get; }

    public Traversal(string id, string name, string description, IReadOnlyList<Transition> transitions)
    {
        Id = id;
        Name = name;
        Description = description;
        Transitions = transitions;
    }
}
