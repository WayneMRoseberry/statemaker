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

    [Fact]
    public void Clone_ReturnsNewInstance()
    {
        var state = new State();
        state.Variables["name"] = "Alice";

        var clone = state.Clone();

        Assert.NotSame(state, clone);
    }

    [Fact]
    public void Clone_CopiesAllVariables()
    {
        var state = new State();
        state.Variables["name"] = "Alice";
        state.Variables["age"] = 30;
        state.Variables["active"] = true;

        var clone = state.Clone();

        Assert.Equal(3, clone.Variables.Count);
        Assert.Equal("Alice", clone.Variables["name"]);
        Assert.Equal(30, clone.Variables["age"]);
        Assert.True((bool)clone.Variables["active"]);
    }

    [Fact]
    public void Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        var state = new State();
        state.Variables["name"] = "Alice";
        state.Variables["count"] = 1;

        var clone = state.Clone();
        clone.Variables["name"] = "Bob";
        clone.Variables["count"] = 99;
        clone.Variables["extra"] = true;

        Assert.Equal("Alice", state.Variables["name"]);
        Assert.Equal(1, state.Variables["count"]);
        Assert.Equal(2, state.Variables.Count);
    }

    [Fact]
    public void Clone_EmptyState()
    {
        var state = new State();

        var clone = state.Clone();

        Assert.NotSame(state, clone);
        Assert.Empty(clone.Variables);
    }

    [Fact]
    public void Equals_SameVariables_ReturnsTrue()
    {
        var a = new State();
        a.Variables["name"] = "Alice";
        a.Variables["age"] = 30;

        var b = new State();
        b.Variables["name"] = "Alice";
        b.Variables["age"] = 30;

        Assert.True(a.Equals(b));
        Assert.True(b.Equals(a));
    }

    [Fact]
    public void Equals_SameVariablesDifferentCase_ReturnsFalse()
    {
        var a = new State();
        a.Variables["name"] = "Alice";
        a.Variables["age"] = 30;

        var b = new State();
        b.Variables["Name"] = "Alice";
        b.Variables["age"] = 30;

        Assert.False(a.Equals(b));
        Assert.False(b.Equals(a));
    }

    [Fact]
    public void Equals_NullValuedVariables_ReturnsTrue()
    {
        var a = new State();
        a.Variables["name"] = null;
        a.Variables["age"] = 30;

        var b = new State();
        b.Variables["name"] = null;
        b.Variables["age"] = 30;

        Assert.True(a.Equals(b));
        Assert.True(b.Equals(a));
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = new State();
        a.Variables["name"] = "Alice";

        var b = new State();
        b.Variables["name"] = "Bob";

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentKeys_ReturnsFalse()
    {
        var a = new State();
        a.Variables["name"] = "Alice";

        var b = new State();
        b.Variables["username"] = "Alice";

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentCount_ReturnsFalse()
    {
        var a = new State();
        a.Variables["name"] = "Alice";
        a.Variables["age"] = 30;

        var b = new State();
        b.Variables["name"] = "Alice";

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_BothEmpty_ReturnsTrue()
    {
        var a = new State();
        var b = new State();

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var a = new State();
        a.Variables["name"] = "Alice";

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void Equals_ObjectOverload_WorksCorrectly()
    {
        var a = new State();
        a.Variables["x"] = 1;

        var b = new State();
        b.Variables["x"] = 1;

        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals("not a state"));
    }

    [Fact]
    public void GetHashCode_EqualStates_SameHash()
    {
        var a = new State();
        a.Variables["name"] = "Alice";
        a.Variables["age"] = 30;

        var b = new State();
        b.Variables["age"] = 30;
        b.Variables["name"] = "Alice";

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_EqualStatesNullValues_SameHash()
    {
        var a = new State();
        a.Variables["name"] = null;
        a.Variables["age"] = 30;

        var b = new State();
        b.Variables["age"] = 30;
        b.Variables["name"] = null;

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }


    [Fact]
    public void GetHashCode_DifferentStates_DifferentHash()
    {
        var a = new State();
        a.Variables["name"] = "Alice";

        var b = new State();
        b.Variables["name"] = "Bob";

        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_EmptyStates_SameHash()
    {
        var a = new State();
        var b = new State();

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_WorksInHashSet()
    {
        var a = new State();
        a.Variables["x"] = 1;

        var b = new State();
        b.Variables["x"] = 1;

        var set = new HashSet<State> { a };

        Assert.Contains(b, set);
        Assert.Single(set);
    }
}
