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
  │   └─ YES → throw ArgumentNullException("No initial state provided")
  │
  ├─ Is config valid?
  │   ├─ Exhaustive: MaxDepth=null AND MaxStates=null → VALID
  │   ├─ State-limited: MaxStates set → VALID
  │   ├─ Dual-limited: Both set → VALID
  │   ├─ Depth-only: MaxDepth set, MaxStates null → INVALID
  │   └─ No config: Both null, not exhaustive → INVALID
  │       └─ throw InvalidOperationException("Invalid configuration: ...")
  │
  └─ Proceed to exploration
```

### 2. Exploration Phase

The builder systematically discovers states using the configured exploration strategy:

```
Initialize:
  - Add initialState to StateMachine as "S0"
  - Add initialState to exploration queue
  - Add initialState to visited set (HashSet<State>)

Explore:
  while queue is not empty:
    currentState = dequeue next state

    for each rule in rules:
      if rule.IsAvailable(currentState):
        newState = rule.Execute(currentState)

        if visited contains equivalent state:
          // Cycle detected - create transition to existing state
          add Transition(currentState → existingState, rule.Name)
        else:
          // New state discovered
          assign unique ID to newState
          add newState to StateMachine
          add newState to visited set
          add Transition(currentState → newState, rule.Name)

          if limits not reached:
            add newState to exploration queue

    check limits:
      if MaxDepth reached → stop adding states beyond this depth
      if MaxStates reached → stop exploration entirely
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

    public void AddState(string stateId, State state);
    public bool RemoveState(string stateId);  // Clears StartingStateId if it matches
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

States are assigned unique IDs during exploration:
- **Default:** Sequential IDs - "S0", "S1", "S2", etc.
- **Custom:** Via `BuilderConfig.GenerateStateIds` function
- **Hash-based:** Optional approach using state content hash

The initial state is always assigned the first ID (e.g., "S0").

## Cycle Detection

Cycle detection relies on `State` equality:

1. Each new state is checked against the visited set using `State.Equals()` and `State.GetHashCode()`
2. If an equivalent state exists, a transition is created to the existing state
3. The duplicate state is NOT added to the exploration queue
4. This prevents infinite loops in state spaces with cycles

### Example

```
S0 (OrderStatus=Pending)
  → ApproveOrder → S1 (OrderStatus=Approved)
  → RejectOrder  → S2 (OrderStatus=Rejected)
    → RetryOrder  → S0 ← cycle detected, transition to existing S0
```

## Logging During Exploration

The builder logs at each phase:

- **INFO:** "Starting exploration from state S0"
- **INFO:** "Discovered new state S1 via rule ApproveOrder"
- **INFO:** "Exploration complete: 5 states, 8 transitions"
- **DEBUG:** "Evaluating rule ApproveOrder against state S0"
- **DEBUG:** "Rule ApproveOrder is available for state S0"
- **DEBUG:** "Cycle detected: new state equals existing state S0"
- **ERROR:** "Rule ApproveOrder threw exception: ..."

## Performance Considerations

- **HashSet<State>** for O(1) duplicate detection
- Efficient `GetHashCode()` implementation on State is critical
- Memory grows linearly with number of states
- `MaxStates` limit is the primary safeguard against memory exhaustion
- For very large state spaces, consider DFS strategy for lower memory overhead

## Related Documentation

- [Declarative Rules Architecture](./declarative-rules.md)
- [Expression Evaluation](./expression-evaluation.md)
- [State Immutability](./state-immutability.md)
- [Export Formats](./export-formats.md)

## References

- PRD Section: State Machine Builder (FR 7-17)
- PRD Section: Configuration Validation (FR 18-24)
- PRD Section: Configuration Options (Design Considerations)
