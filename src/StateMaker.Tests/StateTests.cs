namespace StateMaker.Tests;

public class StateTests
{
    [Fact]
    public void Constructor_CreatesEmptyVariables()
    {
        var state = new State();

        Assert.NotNull(state.Variables);
        Assert.Empty(state.Variables);
    }

    [Fact]
    public void Variables_CanStoreString()
    {
        var state = new State();
        state.Variables["name"] = "Alice";

        Assert.Equal("Alice", state.Variables["name"]);
    }

    [Fact]
    public void Variables_CanStoreInt()
    {
        var state = new State();
        state.Variables["count"] = 42;

        Assert.Equal(42, state.Variables["count"]);
    }

    [Fact]
    public void Variables_CanStoreBool()
    {
        var state = new State();
        state.Variables["active"] = true;

        Assert.True((bool)state.Variables["active"]);
    }

    [Fact]
    public void Variables_CanStoreFloat()
    {
        var state = new State();
        state.Variables["rate"] = 3.14f;

        Assert.Equal(3.14f, state.Variables["rate"]);
    }

    [Fact]
    public void Variables_CanStoreDouble()
    {
        var state = new State();
        state.Variables["price"] = 9.99;

        Assert.Equal(9.99, state.Variables["price"]);
    }

    [Fact]
    public void Variables_CanStoreMultipleTypes()
    {
        var state = new State();
        state.Variables["name"] = "Bob";
        state.Variables["age"] = 30;
        state.Variables["active"] = false;
        state.Variables["score"] = 95.5;

        Assert.Equal(4, state.Variables.Count);
        Assert.Equal("Bob", state.Variables["name"]);
        Assert.Equal(30, state.Variables["age"]);
        Assert.False((bool)state.Variables["active"]);
        Assert.Equal(95.5, state.Variables["score"]);
    }

    [Fact]
    public void Variables_CanChangeValue()
    {
        var state = new State();
        state.Variables["status"] = "Pending";

        state.Variables["status"] = "Active";

        Assert.Equal("Active", state.Variables["status"]);
        Assert.Single(state.Variables);
    }

    [Fact]
    public void Variables_CanRemoveVariable()
    {
        var state = new State();
        state.Variables["name"] = "Alice";
        state.Variables["age"] = 30;

        var removed = state.Variables.Remove("name");

        Assert.True(removed);
        Assert.Single(state.Variables);
        Assert.False(state.Variables.ContainsKey("name"));
        Assert.Equal(30, state.Variables["age"]);
    }
}
