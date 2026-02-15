# PRD: Test Case Generator & Battery Executor (Tasks 3.22, 3.23)

## Objective
- **Task 3.22**: Implement a test case generator that programmatically combines initial state shapes, config combinations, and rule variations to produce build definitions.
- **Task 3.23**: Implement a test battery executor that runs build definitions through the builder and applies oracle checks: no crash/exception, no infinite loop (heuristic timeout), MaxStates and MaxDepth limits respected in output.

## Background
The `StateMachineBuilder` accepts a `State`, `IRule[]`, and `BuilderConfig`. There are many valid combinations of initial state shapes, config limit values, and rule behaviors. Rather than hand-writing every combination, task 3.22 creates a generator that produces `BuildDefinition` objects covering the combinatorial space, and task 3.23 creates an executor that runs them through the builder with automated oracle checks.

## Design

### Task 3.22 — Test Case Generator

#### BuildDefinition Record
A simple data class holding the inputs to `StateMachineBuilder.Build`:
- `string Name` — descriptive label for the test case
- `State InitialState` — the initial state
- `IRule[] Rules` — the rules array
- `BuilderConfig Config` — the builder configuration

#### TestCaseGenerator
A static class with methods that produce `IEnumerable<BuildDefinition>`:

**Initial State Shapes:**
- No variables (empty state)
- One string variable
- One int variable
- One bool variable
- One float/double variable
- Multiple variables (string + int + bool)
- N variables (1..5 int variables)

**Config Combinations (pairwise):**
- MaxStates from: null, 0, -1, 1, 2, 3, 10
- MaxDepth from: null, 0, -1, 1, 2, 3, 10
- ExplorationStrategy: BFS, DFS
- Pairwise selection to avoid full combinatorial explosion

**Rule Variations:**
- Empty rules array
- Single rule: sets a variable to a fixed value
- Single rule: adds a new variable
- Single rule: increments an int variable
- Multiple rules: set + increment
- Rule that is never available (always returns false)

**Generator Method:**
`static IEnumerable<BuildDefinition> GenerateAll()` — produces the cross-product (with pairwise config reduction).

### Task 3.23 — Test Battery Executor

#### TestBatteryExecutor
A static class that takes `IEnumerable<BuildDefinition>` and runs each through the builder with oracle checks.

**Oracle Checks:**
1. **No crash/exception**: `Build()` completes without throwing
2. **No infinite loop**: Wrap `Build()` in a timeout (e.g., 5 seconds) — if exceeded, report as failure
3. **MaxStates respected**: If `config.MaxStates` is set and positive, `result.States.Count <= config.MaxStates` (with allowance for initial state always being added)
4. **MaxDepth respected**: If `config.MaxDepth` is set and positive, no state's minimum path length from start exceeds `config.MaxDepth`
5. **Valid machine**: `result.IsValidMachine()` returns true (when build succeeds)

#### TestBatteryResult
- `string DefinitionName`
- `bool Passed`
- `string? FailureReason`
- `int StateCount`
- `int TransitionCount`

#### Integration as xUnit Tests
- A `[Fact]` test method that generates all definitions, runs the battery, and asserts all pass
- A `[Theory]` with `MemberData` for individual definition results (for granular reporting)

## Files Modified
- `src/StateMaker.Tests/TestCaseGenerator.cs` — new file: BuildDefinition, TestCaseGenerator
- `src/StateMaker.Tests/TestBatteryExecutor.cs` — new file: TestBatteryExecutor, TestBatteryResult
- `src/StateMaker.Tests/TestBatteryTests.cs` — new file: xUnit tests
- `tasks/tasks-state-machine-builder.md` — sub-tasks and completion tracking
