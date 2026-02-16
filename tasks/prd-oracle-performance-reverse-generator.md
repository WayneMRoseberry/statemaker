# PRD: Performance Oracle Checks & Reverse Rule Generator (Tasks 3.24, 3.25)

## Objective
- **Task 3.24**: Add performance oracle checks to the test battery executor: time-to-size ratio within expected bounds, and expected state machine shape matching for tractable cases.
- **Task 3.25**: Implement a reverse rule generator that takes a target state machine shape as input and generates one or more sets of rules that would build it, including variations (extra non-triggering rules, different rule orderings) that should not alter the expected output.

## Background
The existing `TestBatteryExecutor` (task 3.23) runs `BuildDefinition` objects through `StateMachineBuilder.Build` and applies correctness oracle checks (no crash, timeout, MaxStates/MaxDepth limits, IsValidMachine). Task 3.24 extends this with performance-related oracles. Task 3.25 creates a tool that works in the reverse direction: given a desired state machine topology, it generates rules that would produce it.

## Design

### Task 3.24 — Performance Oracle Checks

#### Extended TestBatteryResult
Add a `TimeSpan ElapsedTime` field to `TestBatteryResult` to capture build duration.

#### New Oracle Checks
1. **Time-to-size ratio**: `elapsed.TotalMilliseconds / stateCount` should be below a threshold (e.g., 100ms per state). This is a heuristic — the threshold is generous to avoid flaky failures on slow CI.
2. **Shape matching for tractable cases**: For `BuildDefinition` objects that include an `ExpectedShapeInfo` (optional), verify:
   - Expected state count matches actual
   - Expected transition count matches actual
   - If specified, expected max depth matches actual

#### ExpectedShapeInfo Record
```csharp
public record ExpectedShapeInfo(int? ExpectedStateCount, int? ExpectedTransitionCount, int? ExpectedMaxDepth);
```

#### Extended BuildDefinition
Add an optional `ExpectedShapeInfo? ExpectedShape` field to `BuildDefinition`.

### Task 3.25 — Reverse Rule Generator

#### ReverseRuleGenerator Static Class
Given a target shape descriptor, generates `IRule[]` arrays that would produce that shape when run through `StateMachineBuilder`.

**Target Shapes Supported:**
- **Chain(length)**: Linear chain of N states
- **Cycle(length)**: Cycle of N states
- **ChainThenCycle(chainLength, cycleLength)**: Chain followed by a cycle
- **BinaryTree(depth)**: Complete binary tree of given depth
- **Diamond(branchCount)**: Diverge from root, converge to single endpoint
- **FullyConnected(nodeCount)**: Complete graph with K nodes

**Variations:**
Each shape generator produces:
1. Base rule set (minimal rules to produce the shape)
2. With extra non-triggering rules (rules that never fire, should not alter output)
3. With shuffled rule ordering (different rule orders, should produce same state/transition sets)

**Output:**
`IEnumerable<(string VariationName, State InitialState, IRule[] Rules, ExpectedShapeInfo Expected)>`

#### Integration
- `ReverseRuleGenerator` outputs are fed into `TestBatteryExecutor` as `BuildDefinition` objects with `ExpectedShape` set
- Tests verify that the generated rule sets produce machines matching the expected shape

## Files Modified
- `src/StateMaker.Tests/TestBatteryExecutor.cs` — extend with performance oracles and shape matching
- `src/StateMaker.Tests/TestCaseGenerator.cs` — extend BuildDefinition with ExpectedShapeInfo
- `src/StateMaker.Tests/ReverseRuleGenerator.cs` — new file
- `src/StateMaker.Tests/TestBatteryTests.cs` — new tests for performance and reverse generator
- `tasks/tasks-state-machine-builder.md` — sub-tasks and completion tracking
