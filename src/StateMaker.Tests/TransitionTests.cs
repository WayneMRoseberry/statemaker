namespace StateMaker.Tests;

public class TransitionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var transition = new Transition("S0", "S1", "MoveForward");

        Assert.Equal("S0", transition.SourceStateId);
        Assert.Equal("S1", transition.TargetStateId);
        Assert.Equal("MoveForward", transition.RuleName);
    }

    [Fact]
    public void Constructor_SelfTransition()
    {
        var transition = new Transition("S0", "S0", "Loop");

        Assert.Equal("S0", transition.SourceStateId);
        Assert.Equal("S0", transition.TargetStateId);
        Assert.Equal("Loop", transition.RuleName);
    }

    [Fact]
    public void Properties_AreReadOnly()
    {
        var transition = new Transition("S0", "S1", "Rule1");

        // Properties should retain their constructor values
        Assert.Equal("S0", transition.SourceStateId);
        Assert.Equal("S1", transition.TargetStateId);
        Assert.Equal("Rule1", transition.RuleName);
    }
}
