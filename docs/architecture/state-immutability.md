# State Immutability Architecture

## Overview

State immutability is a fundamental design principle in StateMaker. The `Execute()` method of every rule must return a **new** State object rather than modifying the input state. This ensures correct state comparison, prevents side effects during exploration, and enables reliable cycle detection.

## Why Immutability Matters

### 1. Correct Cycle Detection

The builder maintains a `Dictionary<State, string>` mapping visited states to their IDs. If a rule modifies the input state instead of creating a new one, the dictionary becomes corrupted because the original state object's hash has changed.

**Broken (mutable):**
```csharp
public State Execute(State state)
{
    // BAD: modifies the input state
    state.Variables["Status"] = "Approved";
    return state;  // Returns the SAME object, now modified
}
```

What happens:
1. S0 has `Status = "Pending"`, hash = 12345
2. Rule executes, changes S0 in-place to `Status = "Approved"`, hash = 67890
3. Dictionary now has S0 with a different hash than when it was inserted
4. The original "Pending" state no longer exists for comparison
5. Cycle detection fails because the original state was destroyed

**Correct (immutable):**
```csharp
public State Execute(State state)
{
    // GOOD: creates a new state
    var newState = state.Clone();
    newState.Variables["Status"] = "Approved";
    return newState;  // Returns a NEW object
}
```

What happens:
1. S0 has `Status = "Pending"`, hash = 12345 (unchanged)
2. S1 is created with `Status = "Approved"`, hash = 67890
3. Both states exist independently in the dictionary
4. If another path leads back to `Status = "Pending"`, it correctly matches S0

### 2. Correct State Graph

The builder creates transitions between states. If states are modified in place, multiple transitions end up pointing to the same mutated object rather than distinct state snapshots.

### 3. Predictable Rule Evaluation

All rules are evaluated against each state. If one rule modifies the input state, subsequent rules see the modified state rather than the original, producing unpredictable results.

**Without immutability:**
```
State S0: { Count: 0 }
  Rule A fires: modifies S0 to { Count: 1 }  ← S0 is now changed
  Rule B fires: sees { Count: 1 } instead of { Count: 0 }  ← WRONG
```

**With immutability:**
```
State S0: { Count: 0 }
  Rule A fires: creates S1 { Count: 1 }, S0 unchanged
  Rule B fires: sees S0 { Count: 0 }  ← CORRECT
```

## Implementation Pattern

### State.Clone() Method

The `State` class provides a `Clone()` method to create a copy:

```csharp
public class State : IEquatable<State>
{
    public Dictionary<string, object?> Variables { get; } = new();

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
```

Since state variables are restricted to primitive types (`string`, `int`, `bool`, `float/double`, `null`), a shallow copy of the dictionary is sufficient because primitive types are value types (or immutable in the case of strings).

### Custom Rule Pattern

Every custom rule must follow this pattern:

```csharp
public class MyRule : IRule
{
    public bool IsAvailable(State state)
    {
        // Read-only access to state - never modify
        return state.Variables.ContainsKey("Status")
            && (string)state.Variables["Status"] == "Pending";
    }

    public State Execute(State state)
    {
        // 1. Clone the input state
        var newState = state.Clone();

        // 2. Modify the CLONE, not the input
        newState.Variables["Status"] = "Approved";

        // 3. Return the new state
        return newState;
    }
}
```

### Declarative Rule Immutability

`DeclarativeRule` handles immutability internally:

```csharp
public State Execute(State state)
{
    // DeclarativeRule always clones first
    var newState = state.Clone();

    foreach (var transform in _transformations)
    {
        object newValue = _evaluator.Evaluate(transform.Value, state.Variables);
        newState.Variables[transform.Key] = newValue;
    }

    return newState;
}
```

Note: transformations are evaluated against the **original** state's variables (`state.Variables`), not the new state. This ensures all transformations see the same input values regardless of evaluation order.

## State Equality

Immutability depends on correct equality implementation:

```csharp
public class State : IEquatable<State>
{
    public bool Equals(State? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Variables.Count != other.Variables.Count) return false;

        foreach (var kvp in Variables)
        {
            if (!other.Variables.TryGetValue(kvp.Key, out var otherValue))
                return false;
            if (!Equals(kvp.Value, otherValue))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is State other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var kvp in Variables.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            hash.Add(kvp.Key, StringComparer.Ordinal);
            hash.Add(kvp.Value);
        }
        return hash.ToHashCode();
    }
}
```

**Key points:**
- Variables are compared by key and value
- Hash code must be deterministic (sorted by key)
- Two states with the same variables and values are considered equal
- Only primitive types are compared, so `Equals` on values works correctly

## Testing Immutability

### Test Pattern: Verify Input State Unchanged

```csharp
[Fact]
public void Execute_DoesNotModifyInputState()
{
    var rule = new MyRule();
    var inputState = new State();
    inputState.Variables["Status"] = "Pending";

    // Execute the rule
    var newState = rule.Execute(inputState);

    // Verify input state is unchanged
    Assert.Equal("Pending", inputState.Variables["Status"]);

    // Verify new state is different
    Assert.Equal("Approved", newState.Variables["Status"]);

    // Verify they are different object references
    Assert.NotSame(inputState, newState);
}
```

### Test Pattern: Verify New State Created

```csharp
[Fact]
public void Execute_ReturnsNewStateObject()
{
    var rule = new MyRule();
    var inputState = new State();
    inputState.Variables["Status"] = "Pending";

    var newState = rule.Execute(inputState);

    // Different objects
    Assert.NotSame(inputState, newState);

    // Different dictionaries
    Assert.NotSame(inputState.Variables, newState.Variables);
}
```

## Common Mistakes

### Mistake 1: Returning the Same Object

```csharp
// WRONG
public State Execute(State state)
{
    state.Variables["Status"] = "Done";
    return state;  // Same reference!
}
```

### Mistake 2: Sharing the Dictionary

```csharp
// WRONG
public State Execute(State state)
{
    var newState = new State(state.Variables);  // Shares the same dictionary!
    newState.Variables["Status"] = "Done";      // Modifies both states!
    return newState;
}
```

### Mistake 3: Evaluating Against New State

```csharp
// WRONG (for declarative rules)
public State Execute(State state)
{
    var newState = state.Clone();
    // Using newState.Variables for evaluation means order matters
    newState.Variables["A"] = _evaluator.Evaluate("B + 1", newState.Variables);
    newState.Variables["B"] = _evaluator.Evaluate("A + 1", newState.Variables);
    // B sees the already-modified A!
    return newState;
}

// CORRECT
public State Execute(State state)
{
    var newState = state.Clone();
    // Using state.Variables (original) for evaluation - order doesn't matter
    newState.Variables["A"] = _evaluator.Evaluate("B + 1", state.Variables);
    newState.Variables["B"] = _evaluator.Evaluate("A + 1", state.Variables);
    return newState;
}
```

## Related Documentation

- [State Machine Builder Architecture](./builder-architecture.md)
- [Declarative Rules Architecture](./declarative-rules.md)
- [Expression Evaluation](./expression-evaluation.md)
- [Export Formats](./export-formats.md)

## References

- PRD Section: Custom Rule Implementation (FR 52-56)
- PRD Section: Core Data Structures (FR 1-3)
- PRD Section: Custom Rule Implementation Best Practices (Design Considerations)
