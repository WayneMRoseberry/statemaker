# Test Tools Guide

This document describes the test tools available in the `StateMaker.Tests` project for generating build definitions, executing test batteries, and verifying state machine shapes.

## Overview

Three test tools work together to enable large-scale, automated testing of `StateMachineBuilder`:

| Tool | Purpose | File |
|------|---------|------|
| **TestCaseGenerator** | Generates combinatorial build definitions from initial states, configs, and rule variations | `TestCaseGenerator.cs` |
| **TestBatteryExecutor** | Runs build definitions through the builder and applies oracle checks | `TestBatteryExecutor.cs` |
| **ReverseRuleGenerator** | Generates rules that produce known state machine shapes for shape verification | `ReverseRuleGenerator.cs` |

## TestCaseGenerator

Generates `BuildDefinition` instances by combining initial states, builder configurations, and rule variations.

### Records

```csharp
public record ExpectedShapeInfo(int? ExpectedStateCount, int? ExpectedTransitionCount, int? ExpectedMaxDepth);

public record BuildDefinition(string Name, State InitialState, IRule[] Rules, BuilderConfig Config, ExpectedShapeInfo? ExpectedShape = null);
```

- `BuildDefinition` is the unit of work for all test tools. It bundles everything needed to build a state machine plus optional expected shape information for oracle verification.
- `ExpectedShapeInfo` fields are all nullable — only non-null fields are checked by the shape oracle.

### Generator Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GenerateInitialStates()` | `IEnumerable<State>` | 11 states: empty, single-variable (string, int, bool, double), multi-variable, and 1-5 int variables |
| `GenerateConfigs()` | `IEnumerable<BuilderConfig>` | ~65 configs combining MaxStates (null,0,-1,1,2,3,10), MaxDepth subsets, and BFS/DFS |
| `GenerateRuleVariations()` | `IEnumerable<(string, IRule[])>` | 7 variations: empty, never-available, set-variable, add-variable, increment, set-and-increment, toggle-bool |
| `GenerateAll()` | `IEnumerable<BuildDefinition>` | Cross-product of all states, configs, and rule variations (~5000 definitions) |

### Usage

```csharp
// Generate all definitions and run through battery
var definitions = TestCaseGenerator.GenerateAll();
var results = TestBatteryExecutor.RunAll(definitions);
foreach (var result in results)
{
    Assert.True(result.Passed, $"{result.DefinitionName}: {result.FailureReason}");
}

// Or use individual generators for targeted testing
var configs = TestCaseGenerator.GenerateConfigs();
```

## TestBatteryExecutor

Executes `BuildDefinition` instances and applies a series of oracle checks to the resulting state machine.

### Result Record

```csharp
public record TestBatteryResult(
    string DefinitionName,
    bool Passed,
    string? FailureReason,
    int StateCount,
    int TransitionCount,
    TimeSpan ElapsedTime);
```

### Oracle Checks

The executor applies 7 oracle checks in order. If any oracle fails, the result is returned immediately with `Passed = false` and a descriptive `FailureReason`.

| # | Oracle | What it checks |
|---|--------|----------------|
| 1 | No crash | Build completes without throwing an exception |
| 2 | No infinite loop | Build completes within the timeout (default: 5 seconds) |
| 3 | MaxStates respected | `States.Count <= MaxStates` when MaxStates > 0 |
| 4 | MaxDepth respected | BFS shortest-path depth from start <= MaxDepth when MaxDepth > 0 |
| 5 | Valid machine | `IsValidMachine()` returns true |
| 6 | Performance | Time-to-size ratio does not exceed threshold (default: 100ms/state) |
| 7 | Shape match | State count, transition count, and max depth match `ExpectedShapeInfo` (when specified) |

### Methods

| Method | Description |
|--------|-------------|
| `Run(BuildDefinition)` | Run with default timeout (5s) and performance threshold (100ms/state) |
| `Run(BuildDefinition, TimeSpan timeout)` | Run with custom timeout |
| `Run(BuildDefinition, TimeSpan timeout, double msPerStateThreshold)` | Run with custom timeout and performance threshold |
| `RunAll(IEnumerable<BuildDefinition>)` | Run all definitions with defaults |
| `RunAll(IEnumerable<BuildDefinition>, TimeSpan timeout)` | Run all definitions with custom timeout |

### Usage

```csharp
// Single definition
var result = TestBatteryExecutor.Run(definition);
Assert.True(result.Passed, result.FailureReason);

// With custom timeout
var result = TestBatteryExecutor.Run(definition, TimeSpan.FromSeconds(10));

// Batch run
var results = TestBatteryExecutor.RunAll(definitions);
var failures = results.Where(r => !r.Passed).ToList();
Assert.Empty(failures);
```

## ReverseRuleGenerator

Generates rules designed to produce specific, known state machine shapes. Each shape generator yields multiple `BuildDefinition` variations including the base shape plus rule ordering, split/merge, and non-triggering rule variations.

### Shape Generators

| Method | Shape | States | Transitions | MaxDepth |
|--------|-------|--------|-------------|----------|
| `GenerateChain(length)` | S0 → S1 → ... → Sn | length + 1 | length | length |
| `GenerateCycle(length)` | S0 → S1 → ... → S(n-1) → S0 | length | length | length - 1 |
| `GenerateChainThenCycle(chainLen, cycleLen)` | Chain followed by cycle | chainLen + cycleLen | chainLen + cycleLen | chainLen + cycleLen - 1 |
| `GenerateBinaryTree(depth)` | Full binary tree | 2^(depth+1) - 1 | 2^(depth+1) - 2 | depth |
| `GenerateDiamond(branchCount)` | Root → N branches → converge | branchCount + 2 | branchCount * 2 | 2 |
| `GenerateFullyConnected(nodeCount)` | Every node → every other | nodeCount | nodeCount * (nodeCount-1) | 1 (if >1 node) |

### Variations

Each shape generator yields multiple `BuildDefinition` instances with these variations:

| Variation | Suffix | Description |
|-----------|--------|-------------|
| Base | `_Base` | Original rule set |
| Non-triggering | `_WithNonTrigger` | Adds a rule with `IsAvailable => false` |
| Reversed | `_Reversed` | Rules in reverse order (with non-trigger) |
| Rotated | `_Rotated` | First rule moved to end (for 2+ rules) |
| Interleaved | `_Interleaved` | Odd/even index reordering (for 3+ rules) |
| Split | `_Split` | One broad rule decomposed into N per-state specialized rules sharing the same `GetName()` |
| Split reversed | `_SplitReversed` | Split rules in reverse order |
| Split shuffled | `_SplitShuffled` | Split rules interleaved with non-triggering rules |

All variations include `ExpectedShapeInfo` so the shape oracle in `TestBatteryExecutor` can verify the resulting state machine matches the expected shape.

### Rule Equivalence Concepts

The variations test three axes of rule set equivalence:

1. **Rule ordering**: Different orderings of the same rules produce the same state machine (when MaxStates does not constrain). State IDs may differ but the topology is identical.

2. **Rule split/merge**: A single rule that handles N state values can be replaced by N specialized rules, each handling one value but all sharing the same `GetName()`. The resulting state machine is identical.

3. **Non-triggering rules**: Adding rules that never fire (`IsAvailable => false`) does not change the resulting state machine.

### Usage

```csharp
// Generate all shapes and variations
var allShapes = ReverseRuleGenerator.GenerateAllShapes();
foreach (var definition in allShapes)
{
    var result = TestBatteryExecutor.Run(definition);
    Assert.True(result.Passed, $"{definition.Name}: {result.FailureReason}");
}

// Generate a specific shape
var chains = ReverseRuleGenerator.GenerateChain(5);
foreach (var def in chains)
{
    var result = TestBatteryExecutor.Run(def);
    Assert.True(result.Passed, result.FailureReason);
}
```

### `GenerateAllShapes()` Coverage

`GenerateAllShapes()` produces definitions for all shapes at various sizes:

- Chains: lengths 1, 3, 5, 10
- Cycles: lengths 2, 3, 5
- Chain-then-cycle: (1,2), (2,3), (3,3)
- Binary trees: depths 1, 2, 3
- Diamonds: 2, 3, 4 branches
- Fully connected: 2, 3, 4 nodes

## Combining the Tools

The tools are designed to work together. `TestCaseGenerator` produces broad combinatorial coverage, while `ReverseRuleGenerator` produces targeted shape-specific definitions. Both produce `BuildDefinition` records that `TestBatteryExecutor` can run.

```csharp
// Combinatorial coverage (broad, oracle-based)
var combinatorial = TestCaseGenerator.GenerateAll();

// Shape-specific coverage (targeted, shape-verified)
var shapes = ReverseRuleGenerator.GenerateAllShapes();

// Run everything through the same executor
var allDefinitions = combinatorial.Concat(shapes);
var results = TestBatteryExecutor.RunAll(allDefinitions);
```

## Related Documentation

- [StateMachineBuilder Test Plan](./StateMachineBuilder_test_plan.md)
- [Builder Architecture](../architecture/builder-architecture.md)
