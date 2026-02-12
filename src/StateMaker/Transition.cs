namespace StateMaker;

public class Transition
{
    public string SourceStateId { get; }
    public string TargetStateId { get; }
    public string RuleName { get; }

    public Transition(string sourceStateId, string targetStateId, string ruleName)
    {
        SourceStateId = sourceStateId;
        TargetStateId = targetStateId;
        RuleName = ruleName;
    }
}
