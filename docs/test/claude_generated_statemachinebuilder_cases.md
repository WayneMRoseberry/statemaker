# StateMachineBuilder.Build Test Cases

## Generation Prompt

> You are a QA Engineer working on the StateMachineBuilder.build method
> Write test cases for building state machines from valid, invalid, and complex rule definitions.
> Include cases that push edge conditions on state/transition relationships, and force the builder to contemplate difficult conditions.
> Provide results in a table with columns, Test Case ID, Title, Steps, Expected Results
> Add prioritization

## Input Validation

| Test Case ID | Priority | Title | Steps | Expected Results |
|---|---|---|---|---|
| IV-001 | P0 | Null initial state | Call `Build(null, rules, config)` | Throws `ArgumentNullException` with `ParamName == "initialState"` |
| IV-002 | P0 | Null rules array | Call `Build(initialState, null, config)` | Throws `ArgumentNullException` with `ParamName == "rules"` |
| IV-003 | P0 | Null config | Call `Build(initialState, rules, null)` | Throws `ArgumentNullException` with `ParamName == "config"` |
| IV-004 | P0 | Null element in rules array | Rules array contains a valid rule at index 0 and `null` at index 1 | Throws `ArgumentNullException` with `ParamName == "rules[1]"` |
| IV-005 | P1 | Null element at index 0 of rules array | Rules array has `null` at index 0, valid rule at index 1 | Throws `ArgumentNullException` with `ParamName == "rules[0]"` |
| IV-006 | P1 | All null elements in rules array | Rules array of length 3, all null | Throws `ArgumentNullException` with `ParamName == "rules[0]"` (first null detected) |
| IV-007 | P2 | Empty rules array | Call `Build(initialState, Array.Empty<IRule>(), config)` | Returns `StateMachine` with 1 state (initial), 0 transitions, `IsValidMachine() == true` |

## Basic Build Behavior

| Test Case ID | Priority | Title | Steps | Expected Results |
|---|---|---|---|---|
| BB-001 | P0 | Build returns non-null StateMachine | Build with any valid inputs | Result is not null |
| BB-002 | P0 | Initial state is present in result | Build with initial state having `Variables["status"] = "start"` | `result.States` contains the initial state with matching variables |
| BB-003 | P0 | StartingStateId is set and valid | Build with any valid inputs | `StartingStateId` is non-null and references an existing state in `States` |
| BB-004 | P0 | StartingStateId is "S0" | Build with any valid inputs | `StartingStateId == "S0"` |
| BB-005 | P0 | Result is a valid machine | Build with any valid inputs | `result.IsValidMachine() == true` |
| BB-006 | P0 | Single rule produces new state | Rule always available, changes `status` from "A" to "B" | 2 states, 1 transition from S0 to S1 |
| BB-007 | P1 | Rule unavailable produces no transitions | Rule `IsAvailable` always returns false | 1 state (initial), 0 transitions |
| BB-008 | P1 | Sequential state ID generation | Build with rule producing 3 new states | State IDs are "S0", "S1", "S2", "S3" in order of discovery |

## Cycle Detection

| Test Case ID | Priority | Title | Steps | Expected Results |
|---|---|---|---|---|
| CD-001 | P0 | Clone-of-initial produces self-loop transition | Rule always available, `Execute` returns `Clone()` of input (no changes) | 1 state, 1 transition (self-loop: S0 -> S0) |
| CD-002 | P0 | Toggle cycle records back-transition | Rule toggles `toggle` between true/false, always available | 2 states, 2 transitions (S0 -> S1, S1 -> S0) |
| CD-003 | P0 | No duplicate states on revisit | Rule cycles through values A -> B -> C -> A | 3 states (not 4), transition from C-state back to A-state |
| CD-004 | P1 | Diamond convergence: two paths to same state | Two rules from initial: one sets `x=1`, another sets `y=1`. A third rule sets both `x=1, y=1`. First path arrives at `{x=1, y=1}`, second path finds it already exists | State `{x=1, y=1}` appears once; both transitions point to same state ID |
| CD-005 | P1 | Two rules producing identical output from same state | Two rules both set `value` from 0 to 1 | 2 states, 2 transitions both with `SourceStateId == S0` and `TargetStateId == S1` |
| CD-006 | P1 | Long cycle detected after deep chain | Counter increments 0 -> 1 -> 2 -> ... -> 9, then rule resets to 0 | 10 states, 10 transitions (9 forward + 1 back to S0), exploration terminates |
| CD-007 | P2 | Multiple independent cycles | Two variables each toggle independently. `a` toggles true/false, `b` toggles true/false | 4 states (TT, TF, FT, FF), all transitions valid, no duplicates |
| CD-008 | P2 | Self-loop on non-initial state | Rule increments counter, second rule available only when counter == 2 produces clone (no change) | Self-loop transition on the counter==2 state, not on initial |

## Exploration Strategy

| Test Case ID | Priority | Title | Steps | Expected Results |
|---|---|---|---|---|
| ES-001 | P0 | BFS explores level by level | Two rules: +1 and +10 on counter, `MaxStates = 5`, BFS | States contain values {0, 1, 10, 2, 11} -- both children of root explored before grandchildren |
| ES-002 | P0 | DFS explores depth-first | Two rules: +1 and +10 on counter, `MaxStates = 5`, DFS | States contain values {0, 1, 2, 10, 11} -- first rule's branch explored deeply first |
| ES-003 | P1 | BFS default when ExplorationStrategy not set | Build with default `BuilderConfig()` and two branching rules, `MaxStates = 5` | Same result as explicit `BREADTHFIRSTSEARCH` |
| ES-004 | P1 | BFS and DFS produce same states when unbounded | Two rules, cycle terminates naturally (both paths converge). No limits set | Both strategies produce the same set of states and transitions (order of IDs may differ) |
| ES-005 | P2 | DFS with MaxStates cuts off different states than BFS | Three rules producing branching tree, `MaxStates = 4` | DFS and BFS produce different state value sets due to different exploration order |

## Depth Limiting

| Test Case ID | Priority | Title | Steps | Expected Results |
|---|---|---|---|---|
| DL-001 | P0 | MaxDepth limits exploration depth | Counter rule, `MaxDepth = 2` | 3 states (depth 0, 1, 2), states at depth 2 are not explored further |
| DL-002 | P0 | MaxDepth = 0 prevents all exploration | Counter rule always available, `MaxDepth = 0` | 1 state (initial only), 0 transitions -- initial state is added but not explored |
| DL-003 | P1 | MaxDepth = 1 explores only initial state's children | Counter rule, `MaxDepth = 1` | 2 states (depth 0 and 1), transitions only from S0 |
| DL-004 | P1 | MaxDepth does not prevent cycle-back transitions at depth boundary | Toggle rule, `MaxDepth = 1`. Initial at depth 0 produces toggled state at depth 1. At depth 1, exploration stops | 2 states, 1 transition (S0 -> S1). No back-transition because depth-1 state is not explored |
| DL-005 | P1 | MaxDepth with branching tree | Two rules, `MaxDepth = 2`. Tree fans out at each level | States exist at depths 0, 1, and 2. No states at depth 3 |
| DL-006 | P2 | MaxDepth with cycle at boundary depth | Rule increments counter mod 3 (0->1->2->0). `MaxDepth = 2`. State at depth 2 has counter==2, not explored, so cycle-back to counter==0 never recorded | 3 states, 2 transitions (0->1, 1->2). No transition from 2 back to 0 |
| DL-007 | P2 | MaxDepth null means unlimited depth | Counter rule, `MaxStates = 100`, `MaxDepth = null` | Exploration continues until MaxStates reached, not limited by depth |

## State Count Limiting

| Test Case ID | Priority | Title | Steps | Expected Results |
|---|---|---|---|---|
| SL-001 | P0 | MaxStates limits total states | Counter rule, `MaxStates = 3` | Exactly 3 states |
| SL-002 | P0 | MaxStates = 1 returns only initial state | Counter rule always available, `MaxStates = 1` | 1 state (initial), 0 transitions -- new states rejected because limit already reached |
| SL-003 | P1 | MaxStates reached mid-rule-loop | Two rules both available, `MaxStates = 2`. First rule produces new state (count reaches 2). Second rule would produce another new state | 2 states; second rule's new state not added because limit reached |
| SL-004 | P1 | MaxStates does not prevent cycle transitions | Two rules both available, one produces duplicate (cycle). `MaxStates = 2` | Cycle transition to existing state is still recorded even though limit applies to new states |
| SL-005 | P2 | MaxStates null means unlimited states | Toggle rule (self-terminating at 2 states), `MaxStates = null` | Exploration runs to completion naturally (2 states) |
| SL-006 | P2 | MaxStates and MaxDepth both set, MaxStates triggers first | Counter rule, `MaxDepth = 100`, `MaxStates = 3` | Exactly 3 states; depth limit never reached |
| SL-007 | P2 | MaxStates and MaxDepth both set, MaxDepth triggers first | Counter rule, `MaxDepth = 2`, `MaxStates = 100` | 3 states (depths 0, 1, 2); state limit never reached |

## Transition Relationships

| Test Case ID | Priority | Title | Steps | Expected Results |
|---|---|---|---|---|
| TR-001 | P0 | Linear chain transitions | Counter 0->1->2->3, `MaxStates = 4` | 3 transitions: S0->S1, S1->S2, S2->S3. Each source is previous state's target |
| TR-002 | P0 | Branching transitions from single state | Two rules available only on initial state, each produces unique state | 2 transitions, both with `SourceStateId == S0`, different targets |
| TR-003 | P0 | Transition RuleName matches rule class name | Build with TestRule class | All `transition.RuleName == "TestRule"` |
| TR-004 | P1 | Multiple transitions between same state pair | Two different rules both produce the same state from the same source | 2 transitions with same Source/Target but potentially different RuleNames |
| TR-005 | P1 | Self-loop transition | Rule available on state, Execute returns equivalent clone | Transition where `SourceStateId == TargetStateId` |
| TR-006 | P2 | Fan-in: multiple sources to single target | Three source states all have a rule that produces the same target state | 3 transitions all with same `TargetStateId`, different `SourceStateId` values |
| TR-007 | P2 | No orphan transitions | Build any complex graph with cycles and limits | Every `transition.SourceStateId` and `transition.TargetStateId` exists in `States` (guaranteed by `IsValidMachine`) |

## Complex Rule Interactions

| Test Case ID | Priority | Title | Steps | Expected Results |
|---|---|---|---|---|
| CR-001 | P0 | Rule conditionally available based on state variables | Rule available only when `counter < 4`, increments counter | 5 states (0-4), 4 transitions. Exploration terminates naturally when counter == 4 |
| CR-002 | P1 | Multiple rules with different availability conditions | Rule A available when `phase == "init"`, sets `phase = "running"`. Rule B available when `phase == "running"`, sets `phase = "done"` | 3 states (init, running, done), 2 transitions forming a linear chain |
| CR-003 | P1 | Rule available on some states but not others in a branch | Two branching rules from initial. Rule A leads to state where Rule C is available. Rule B leads to state where Rule C is not available | Rule C applied only on one branch, asymmetric tree |
| CR-004 | P1 | Many rules, only one available per state | 5 rules, each available only on a specific counter value (0, 1, 2, 3, 4). Each increments counter | Linear chain of 6 states, 5 transitions. Same as a single rule but verifies multi-rule iteration |
| CR-005 | P2 | Rule that adds a new variable to the state | Initial state has `{a: 1}`. Rule adds `{b: 2}` via clone | New state has 2 variables, differs from initial, creates new state |
| CR-006 | P2 | Rule that changes variable type | Initial state has `{value: 0}` (int). Rule sets `value = "zero"` (string) | New state created because value differs. Both states present |
| CR-007 | P2 | Large fan-out: many rules all available on initial state | 10 rules, each setting a different variable to a unique value, all available on initial | 11 states (initial + 10), 10 transitions from S0, `MaxStates` not set so all explored |
| CR-008 | P3 | Rule produces state with empty variables | Initial state has `{status: "start"}`. Rule clones and clears all variables | 2 states: initial with variables, new state with empty variables dictionary |

## Edge Conditions

| Test Case ID | Priority | Title | Steps | Expected Results |
|---|---|---|---|---|
| EC-001 | P1 | Initial state has no variables | Build with `new State()` (empty variables), rule returns clone | 1 state (clone is equal to initial), 1 self-loop transition |
| EC-002 | P1 | Initial state with many variables | Initial state with 50 key-value pairs, rule changes one | 2 states, equality check works correctly with large variable count |
| EC-003 | P1 | State variable with null value | Initial state has `Variables["x"] = null`. Rule sets `x = 1` | 2 states, null-valued state and non-null-valued state treated as different |
| EC-004 | P2 | MaxDepth = 1 with MaxStates = 1 | Counter rule, both limits set | 1 state (MaxStates triggers immediately), 0 transitions |
| EC-005 | P2 | MaxStates = 2, multiple rules, first rule fills limit | Three available rules, `MaxStates = 2`. First rule produces new state (count == 2) | 2 states, 1 transition. Second and third rules never add states |
| EC-006 | P2 | Large state space with natural termination | Binary counter with 3 bits, rules flip each bit. All 8 combinations reachable | 8 states, 24 transitions (3 rules x 8 states, all producing existing states eventually). Terminates without limits |
| EC-007 | P2 | State equality with different variable insertion order | Two rules produce states with same keys/values but added in different order | Treated as same state (equality ignores insertion order). Only one state instance in result |
| EC-008 | P3 | Rule whose Execute returns the same object reference (not a clone) | Rule returns the input state directly instead of cloning | Self-loop transition recorded. No new state added. Same behavior as clone that matches |
| EC-009 | P3 | MaxStates very large (e.g., int.MaxValue) | Counter rule, MaxStates = int.MaxValue, MaxDepth = 3 | Depth limit triggers naturally. MaxStates effectively unused |
| EC-010 | P3 | Single rule available on all states, produces unique states | Counter increments unboundedly, `MaxStates = 1000` | Exactly 1000 states, 999 transitions, linear chain |

## Priority Summary

| Priority | Count | Description |
|---|---|---|
| P0 | 17 | Core functionality that must work for any valid use |
| P1 | 18 | Important edge cases, boundary conditions, and limit interactions |
| P2 | 17 | Complex scenarios, multi-rule interactions, stress conditions |
| P3 | 4 | Unusual but theoretically valid edge cases |