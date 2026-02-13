# StateMachineBuilder Test Plan

## Overview

`StateMachineBuilder` is the core engine of the StateMaker library. It takes an initial state, an array of rules, and a configuration object, then explores the state space by repeatedly applying rules to discovered states. The result is a `StateMachine` containing all reachable states and the transitions between them.

## Method Under Test

```csharp
public class StateMachineBuilder : IStateMachineBuilder
{
    public StateMachine Build(State initialState, IRule[] rules, BuilderConfig config);
}
```

### Interface

```csharp
public interface IStateMachineBuilder
{
    StateMachine Build(State initialState, IRule[] rules, BuilderConfig config);
}
```

## Data Structures

### State

Represents a single state in the state machine. Contains a dictionary of named variables with primitive values.

```csharp
public class State : IEquatable<State>
{
    public Dictionary<string, object?> Variables { get; }

    public State Clone();
    public bool Equals(State? other);
    public override bool Equals(object? obj);
    public override int GetHashCode();
}
```

- `Variables`: Key-value pairs holding the state data. Supports primitive types: `string`, `int`, `bool`, `float`/`double`, and `null`.
- `Clone()`: Creates a shallow copy of the state with the same variable keys and values.
- Equality is value-based: two states are equal if they have the same keys with the same values.
- `GetHashCode()` uses sorted keys for deterministic hashing.

### IRule

Defines the contract for a rule that can be applied to a state to produce a new state.

```csharp
public interface IRule
{
    bool IsAvailable(State state);
    State Execute(State state);
}
```

- `IsAvailable(State)`: Returns `true` if the rule can be applied to the given state.
- `Execute(State)`: Applies the rule to produce a new state. Should not mutate the input state.

### BuilderConfig

Configuration options that control how the builder explores the state space.

```csharp
public class BuilderConfig
{
    public int? MaxDepth { get; set; }
    public int? MaxStates { get; set; }
    public ExplorationStrategy ExplorationStrategy { get; set; }
    public LogLevel LogLevel { get; set; }
}
```

- `MaxDepth` (default `null`): Maximum exploration depth from the initial state. States at this depth are added but not explored further. `null` means no depth limit.
- `MaxStates` (default `null`): Maximum number of states in the result. Once reached, no new states are added. `null` means no state count limit.
- `ExplorationStrategy` (default `BREADTHFIRSTSEARCH`): Controls the order in which states are explored.
- `LogLevel` (default `INFO`): Controls logging verbosity.

### ExplorationStrategy

```csharp
public enum ExplorationStrategy
{
    BREADTHFIRSTSEARCH,
    DEPTHFIRSTSEARCH
}
```

- `BREADTHFIRSTSEARCH`: Explores states level by level (queue-based).
- `DEPTHFIRSTSEARCH`: Explores one branch fully before backtracking (stack-based).

### StateMachine

The output of the builder. Contains the discovered states, transitions between them, and the starting state identifier.

```csharp
public class StateMachine
{
    public IReadOnlyDictionary<string, State> States { get; }
    public string? StartingStateId { get; set; }
    public List<Transition> Transitions { get; }

    public void AddState(string stateId, State state);
    public bool RemoveState(string stateId);
    public bool IsValidMachine();
}
```

- `States`: Read-only dictionary mapping state IDs (e.g., "S0", "S1") to `State` objects.
- `StartingStateId`: The ID of the initial state. Setting this to a non-existent state ID throws `StateDoesNotExistException`.
- `Transitions`: List of all transitions discovered during exploration.
- `AddState(stateId, state)`: Adds a state to the internal dictionary.
- `RemoveState(stateId)`: Removes a state; clears `StartingStateId` if it matches.
- `IsValidMachine()`: Returns `true` if the machine has at least one state, a non-null `StartingStateId`, and all transitions reference existing states.

### Transition

Represents a directed edge between two states, labeled with the rule that produced it.

```csharp
public class Transition
{
    public string SourceStateId { get; }
    public string TargetStateId { get; }
    public string RuleName { get; }
}
```

- `SourceStateId`: The state the rule was applied to.
- `TargetStateId`: The state produced by the rule.
- `RuleName`: Derived from the rule's class name (`rule.GetType().Name`).

### StateDoesNotExistException

```csharp
public class StateDoesNotExistException : Exception
```

Thrown when `StartingStateId` is set to a state ID that does not exist in the `States` dictionary.

## Builder Behavior Summary

1. **Input validation**: Guards against null `initialState`, `rules`, `config`, and null elements within the `rules` array. Throws `ArgumentNullException` on violations.
2. **Initialization**: Adds the initial state as "S0" and sets it as `StartingStateId`.
3. **Exploration loop**: Uses a `LinkedList` as a unified frontier (FIFO for BFS, LIFO for DFS). For each state taken from the frontier, applies every rule:
   - If the rule is not available (`IsAvailable` returns `false`), skip it.
   - If the rule produces a state already visited (detected via `HashSet<State>` using value equality), record a transition to the existing state but do not re-explore.
   - If the rule produces a new state, add it to the machine, record the transition, and add it to the frontier for further exploration.
4. **Limits**: `MaxDepth` prevents exploring states beyond a configured depth. `MaxStates` stops adding new states once the count is reached.
5. **State IDs**: Generated sequentially as "S0", "S1", "S2", etc.
6. **Output**: Returns a `StateMachine` that satisfies `IsValidMachine() == true`.

## Test Sections

_(to be populated)_