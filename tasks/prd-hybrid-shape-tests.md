# PRD: Hybrid Shape Tests (Task 3.16)

## Objective
Write tests with rules designed to produce hybrid shapes combining multiple topologies (chains + cycles, branches + cycles, multiple shape neighborhoods).

## Background
Tasks 3.12-3.15 verified individual shape categories (chains, cycles, complex cycles, branches, reconnecting branches, fully connected graphs). Task 3.16 combines these topologies to verify the builder correctly handles graphs that contain multiple distinct shape regions in a single state machine.

## Test Categories

### 1. Chain + Cycle Hybrids
Tests where a chain leads into a cycle, or a chain branches into both a terminal path and a cycle.
- Chain prefix followed by a cycle (already covered in 3.13 as chain-then-cycle, but here we combine with other shapes)
- Chain that branches: one branch terminates, other enters a cycle
- Two independent chain-then-cycle segments reachable from a common root

### 2. Branch + Cycle Hybrids
Tests where branching structures contain cycles in one or more branches.
- Root branches to a terminal chain and a cycle
- Root branches to two independent cycles of different lengths
- Tree structure where leaf nodes enter cycles
- Diamond shape where convergence point enters a cycle

### 3. Multiple Shape Neighborhoods
Tests where the graph contains distinct topological regions connected by transitions.
- Chain -> branch -> cycle (three-phase topology)
- Diamond with one branch containing a cycle and other branch being a chain
- Fully connected sub-graph reachable from a chain prefix
- Two diamonds connected by a chain, with a cycle at the end

### 4. Complex Hybrid Compositions
Tests combining three or more distinct topologies.
- Branch where each arm has a different topology (chain, cycle, diamond)
- Chain -> diamond -> cycle -> terminal
- Nested structure: outer cycle with inner branch containing a sub-cycle

## Assertions
- Correct total state and transition counts
- All states reachable from start
- `IsValidMachine()` returns true
- Specific structural properties (e.g., cycle detection, convergence points, terminal states)

## Files Modified
- `src/StateMaker.Tests/StateMachineShapeTests.cs` — new test region and test methods
- `tasks/tasks-state-machine-builder.md` — sub-tasks and completion tracking
