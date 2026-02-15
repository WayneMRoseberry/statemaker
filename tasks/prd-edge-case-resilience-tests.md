# PRD: Edge Case and Resilience Tests (Tasks 3.20, 3.21)

## Objective
- **Task 3.20**: Write tests for rule behavior edge cases: rules that mutate the input state passed to `Execute` (violating immutability), verifying the builder handles or detects this.
- **Task 3.21**: Write tests for resilience: null rules array, null elements within rules array, null config, and building from configurations with contradictory or nonsensical limits (e.g., MaxStates=0, MaxDepth=-1).

## Background
The `StateMachineBuilder` relies on `State` equality and hashing for cycle detection and deduplication. If a rule mutates the input state instead of cloning, the builder's internal `HashSet<State>` and `Dictionary<State, string>` can become corrupted. Task 3.20 tests this scenario. Task 3.21 tests that the builder handles edge-case configurations gracefully (null inputs already throw `ArgumentNullException`; nonsensical numeric limits like MaxStates=0 or MaxDepth=-1 need verification).

## Test Categories

### Task 3.20 — Input State Mutation
- Rule that modifies the input state's variables directly (no clone)
- Rule that partially clones then mutates original
- Verify builder still produces a valid machine or detects the violation
- Verify the original state in the machine is not corrupted after build

### Task 3.21 — Resilience
- Null inputs: null initial state, null rules array, null config, null element in rules (already tested, but verify in new test class for completeness)
- MaxStates=0: should produce at least the initial state (1 state) since initial is always added
- MaxStates=1: should produce exactly 1 state
- MaxDepth=0: should produce initial state with no exploration
- MaxDepth=-1: negative depth — verify behavior
- MaxStates=-1: negative states — verify behavior
- Empty initial state (no variables)
- Both MaxStates and MaxDepth set to 1

## Files Modified
- `src/StateMaker.Tests/StateMachineBuilderTests.cs` — new test methods for mutation and resilience
- `tasks/tasks-state-machine-builder.md` — sub-tasks and completion tracking
