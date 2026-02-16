# State Machine Builder Architecture

## Overview

The `StateMachineBuilder` is the core engine of StateMaker. It takes an initial state, a set of rules, and a configuration, and produces a `StateMachine` containing all discovered states and transitions.

## Component Overview

### Input
- **Initial State (`State`):** Starting point for exploration
- **Rules (`IRule[]`):** Array of rules (custom, declarative, or mixed)
- **Configuration (`BuilderConfig`):** Exploration limits, strategy, and logging settings

### Output
- **StateMachine:** Contains all discovered states, transitions, and the starting state ID

## Builder Workflow

### 1. Validation Phase

Before exploration begins, the builder validates inputs:

```
Build(initialState, rules, config)
  │
  ├─ Is initialState null?
  │   └─ YES → throw ArgumentNullException
  │
  ├─ Is rules null?
  │   └─ YES → throw ArgumentNullException
  │
  ├─ Is config null?
  │   └─ YES → throw ArgumentNullException
  │
  ├─ Does rules contain any null elements?
  │   └─ YES → throw ArgumentNullException("rules[i]", "Rule at index i is null.")
  │
  └─ Proceed to exploration
```

### 2. Exploration Phase

The builder systematically discovers states using the configured exploration strategy:

```
Initialize:
  - Add initialState to StateMachine as "S0"
  - Add initialState to frontier (LinkedList)
  - Map initialState → "S0" in stateToId (Dictionary<State, string>)

Explore:
  while frontier is not empty:
    currentState = remove from frontier (last for DFS, first for BFS)

    if MaxDepth reached for currentState → skip

    for each rule in rules:
      if rule.IsAvailable(currentState):
        newState = rule.Execute(currentState)
        ruleName = rule.GetName()

        if stateToId contains equivalent state:
          // Cycle detected - create transition to existing state
          add Transition(currentState → existingState, ruleName)
        else:
          if MaxStates reached → break out of rule loop

          // New state discovered
          assign unique ID to newState
          add newState to StateMachine
          map newState → newId in stateToId
          add Transition(currentState → newState, ruleName)
          add newState to frontier
```

### 3. Result Assembly

After exploration completes, the builder returns a `StateMachine` containing:
- All discovered states with unique IDs
- All transitions with source ID, target ID, and rule name
- The starting state ID

## Exploration Strategies

### Breadth-First Search (BFS) - Default

Uses a queue (FIFO) for exploration. Discovers states level by level.

```
Level 0: [S0]
Level 1: [S1, S2, S3]        ← all states reachable in 1 step
Level 2: [S4, S5, S6, S7]    ← all states reachable in 2 steps
Level 3: [S8, S9]            ← all states reachable in 3 steps
```

**Advantages:**
- Finds shortest path to any state
- Depth limit is meaningful (exactly N transitions from start)
- Predictable exploration order

### Depth-First Search (DFS)

Uses a stack (LIFO) for exploration. Explores one path fully before backtracking.

```
S0 → S1 → S4 → S8 (dead end, backtrack)
              → S9 (dead end, backtrack)
         → S5 (dead end, backtrack)
    → S2 → S6 → ...
```

**Advantages:**
- Lower memory usage for deep, narrow state spaces
- Finds deep states faster

## Key Data Structures

### StateMachine

```csharp
public class StateMachine
{
    public IReadOnlyDictionary<string, State> States { get; }
    public string? StartingStateId { get; set; }  // Validates state exists; throws StateDoesNotExistException
    public List<Transition> Transitions { get; }

    public void AddOrUpdateState(string stateId, State state);
    public bool RemoveState(string stateId);  // Clears StartingStateId if it matches
    // Note: RemoveState does not remove transitions referencing the removed state.
    // It is the caller's responsibility to manage transitions after removal.
    public bool IsValidMachine();  // True if ≥1 state, non-null StartingStateId, all transitions reference existing states
}
```

### Transition

```csharp
public class Transition
{
    public string SourceStateId { get; }
    public string TargetStateId { get; }
    public string RuleName { get; }
}
```

### BuilderConfig

```csharp
public class BuilderConfig
{
    public int? MaxDepth { get; set; }
    public int? MaxStates { get; set; }
    public ExplorationStrategy ExplorationStrategy { get; set; }  // BREADTHFIRSTSEARCH or DEPTHFIRSTSEARCH
    public LogLevel LogLevel { get; set; }
}
```

## State ID Generation

States are assigned sequential unique IDs during exploration: "S0", "S1", "S2", etc.

The initial state is always assigned "S0".

## Cycle Detection

Cycle detection relies on `State` equality via the `stateToId` dictionary:

1. Each new state is checked against the `stateToId` dictionary using `State.Equals()` and `State.GetHashCode()`
2. If an equivalent state exists, a transition is created to the existing state (using its known ID)
3. The duplicate state is NOT added to the frontier
4. This prevents infinite loops in state spaces with cycles

### Example

```
S0 (OrderStatus=Pending)
  → ApproveOrder → S1 (OrderStatus=Approved)
  → RejectOrder  → S2 (OrderStatus=Rejected)
    → RetryOrder  → S0 ← cycle detected, transition to existing S0
```

## Performance Considerations

- **Dictionary<State, string>** (`stateToId`) for O(1) duplicate detection and state-to-ID mapping
- Efficient `GetHashCode()` implementation on State is critical
- Memory grows linearly with number of states
- `MaxStates` limit is the primary safeguard against memory exhaustion
- For very large state spaces, consider DFS strategy for lower memory overhead

## Known Behaviors

### Duplicate Transitions
When two rules with the same `GetName()` produce the same target state from the same source state, the builder creates duplicate transitions (same source, target, and name). This is by design — the builder does not deduplicate transitions.

### Rule Order at MaxStates Boundary
When `MaxStates` constrains the exploration, rule order can affect the resulting state machine structure. The first rule that generates a new state gets added; subsequent rules that would generate different new states are skipped once the limit is reached. Without `MaxStates` constraints, rule order does not affect the set of states and transitions (though state IDs may differ).

## Related Documentation

- [Declarative Rules Architecture](./declarative-rules.md)
- [Expression Evaluation](./expression-evaluation.md)
- [State Immutability](./state-immutability.md)
- [Export Formats](./export-formats.md)

## References

- PRD Section: State Machine Builder (FR 7-17)
- PRD Section: Configuration Validation (FR 18-24)
- PRD Section: Configuration Options (Design Considerations)
