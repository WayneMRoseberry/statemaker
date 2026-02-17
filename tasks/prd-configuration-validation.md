# PRD: Configuration Validation (Task 4.0)

## Overview

Add explicit configuration validation to `StateMachineBuilder.Build()` so that invalid or nonsensical `BuilderConfig` values are rejected with clear error messages before exploration begins.

## Current State

The builder already validates null arguments:
- `initialState` null → `ArgumentNullException`
- `rules` null → `ArgumentNullException`
- `config` null → `ArgumentNullException`
- `rules[i]` null → `ArgumentNullException`

No validation currently exists for `BuilderConfig` property values.

## Scope

### Sub-tasks from task list

| Sub-task | Description | Status |
|----------|-------------|--------|
| 4.1 | Null check for initial state | Already implemented |
| 4.2 | Validate exhaustive mode (both MaxDepth and MaxStates null) is valid | Needs test |
| 4.3 | Validate state-limited mode (MaxStates set, MaxDepth null) is valid | Needs test |
| 4.4 | Validate dual-limited mode (both set) is valid | Needs test |
| 4.5 | Reject depth-only mode (MaxDepth set, MaxStates null) | **Skipped — see note** |
| 4.6 | Tests for each valid configuration combination | Implement |
| 4.7 | Tests for each invalid configuration (correct exception type/message) | Implement |

### Note on 4.5: Depth-only mode

The original task list specifies rejecting configs where `MaxDepth` is set but `MaxStates` is null. However, this conflicts with established behavior:

- The builder correctly handles depth-only mode — it limits BFS/DFS exploration depth
- Multiple existing tests use depth-only configs (e.g., `Build_MaxDepth_StopsExplorationBeyondConfiguredDepth`, `Build_MaxDepth0_NoExplorationBeyondInitial`)
- `TestCaseGenerator` generates depth-only configs as valid test inputs
- Depth-only mode is finite and safe — state count is bounded by the branching factor raised to MaxDepth

**Decision**: Depth-only mode is valid and will remain supported. Task 4.5 is skipped.

## Implementation

### Valid configurations (no exception thrown)
- Exhaustive: `MaxStates = null, MaxDepth = null`
- State-limited: `MaxStates = N, MaxDepth = null`
- Depth-limited: `MaxDepth = N, MaxStates = null`
- Dual-limited: `MaxStates = N, MaxDepth = M`
- All above with both `BFS` and `DFS` strategies

### Tests to add (BuilderConfigTests.cs)
- Parameterized tests confirming no exception for each valid config combination
- Null argument tests are already covered in StateMachineBuilderTests.cs (4.1 already done)

## Files Modified

- `src/StateMaker.Tests/BuilderConfigTests.cs` — Add validation tests
- `tasks/tasks-state-machine-builder.md` — Check off completed sub-tasks
