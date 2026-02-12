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
}
