# PRD: Exploration Strategy Equivalence Tests (Task 3.17)

## Objective
Write tests verifying exploration strategy equivalence: same initial state, rules, and config must produce the same state machine (same states and transitions) under both BFS and DFS.

## Background
The `StateMachineBuilder` supports two exploration strategies: `BREADTHFIRSTSEARCH` and `DEPTHFIRSTSEARCH`. While the traversal order differs (affecting state ID assignment), both strategies should discover the same set of states (by variable values) and the same set of transitions (by source/target state variables and rule name). This task verifies that structural equivalence holds across all previously tested shape categories.

## Key Insight
State IDs (S0, S1, S2...) are assigned sequentially as states are discovered, so BFS and DFS will produce different ID assignments. Equivalence must be checked structurally:
- Same number of states
- Same set of state variable dictionaries
- Same number of transitions
- Same set of transitions when mapped through state variables (not IDs)

## Test Categories

### 1. Simple Shape Equivalence
- Single state (trivially equivalent)
- Chain shapes of varying lengths
- Simple cycle shapes

### 2. Branching Shape Equivalence
- Binary tree structures
- Fan-out branches
- Connected sub-branches (deduplication)

### 3. Complex Shape Equivalence
- Chain-then-cycle
- Diamonds (reconverging branches)
- Nested cycles

### 4. Hybrid Shape Equivalence
- Branch + cycle hybrids
- Multi-topology compositions
- Fully connected subgraphs

## Assertions
- `AssertStrategyEquivalence` helper: builds with BFS and DFS, then compares:
  - Equal state counts
  - Same set of state variable dictionaries
  - Equal transition counts
  - Same set of (sourceVars, targetVars, ruleName) transition triples

## Files Modified
- `src/StateMaker.Tests/StateMachineShapeTests.cs` — new test region and helper
- `tasks/tasks-state-machine-builder.md` — sub-tasks and completion tracking
