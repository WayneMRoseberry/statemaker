# StateMachineBuilder.Build() Test Cases (Chain of Thought)

## Prompt

> You are a QA Engineer working on the StateMachineBuilder.build method. Let's think step by step. How would you design test cases for building state machines from valid, invalid, and complex rule definitions.
> Output: (different combinations of inputs and what they are intended to cover, case ids, titles, descriptions ) -> Final Answer: Full test case list.

---

## Inputs Under Test

| Parameter | Type | Key Variations |
|---|---|---|
| `initialState` | `State` | null, empty variables, single variable, many variables, null-valued variables |
| `rules` | `IRule[]` | null, empty, single rule, multiple rules, null elements, conditional rules |
| `config` | `BuilderConfig` | null, defaults, MaxDepth (null/0/1/N), MaxStates (null/1/2/N), BFS/DFS |

## State Space Shapes Covered

Linear chain, self-loop, 2-state toggle, diamond convergence, fan-out, fan-in, deep tree, wide tree, disconnected branches (via conditional rules), and combinations under depth/state limits.

---

## Category 1: Input Validation (IV)

| ID | Title | Description | Covers |
|---|---|---|---|
| **IV-001** | Null initialState throws | `Build(null, rules, config)` -> `ArgumentNullException` | Null guard on first parameter |
| **IV-002** | Null rules array throws | `Build(state, null, config)` -> `ArgumentNullException` | Null guard on second parameter |
| **IV-003** | Null config throws | `Build(state, rules, null)` -> `ArgumentNullException` | Null guard on third parameter |
| **IV-004** | Null rule at index 0 | `rules[0]` is null -> `ArgumentNullException` with message containing `"rules[0]"` | Per-element null check, first element |
| **IV-005** | Null rule at middle index | `rules = [valid, null, valid]` -> exception message contains `"rules[1]"` | Per-element null check, non-boundary index |
| **IV-006** | Null rule at last index | `rules = [valid, valid, null]` -> exception references last index | Per-element null check, last element |
| **IV-007** | Multiple null rules, first caught | `rules = [null, null]` -> exception references index 0, not index 1 | Validation stops at first null (loop-order guarantee) |

---

## Category 2: Basic Build Behavior (BB)

| ID | Title | Description | Covers |
|---|---|---|---|
| **BB-001** | Build returns non-null | Any valid inputs -> result is not null | Return value existence |
| **BB-002** | Result is always valid machine | Any valid inputs -> `result.IsValidMachine() == true` | Output structural guarantee |
| **BB-003** | Initial state assigned "S0" | `result.StartingStateId == "S0"` and `result.States["S0"]` equals the input initial state | Starting state ID convention |
| **BB-004** | Empty rules array, no transitions | `rules = []` -> machine has 1 state ("S0"), 0 transitions | Minimal valid build |
| **BB-005** | State IDs are sequential | Given rules producing 4 new states -> IDs are "S0", "S1", "S2", "S3", "S4" | ID generation pattern |
| **BB-006** | Initial state with empty variables | `initialState.Variables` is empty -> builds successfully, state preserved | Empty state handling |
| **BB-007** | Initial state with multiple variables | `initialState.Variables = {a:1, b:"x", c:true}` -> state in result has same variables | Multi-variable state preservation |
| **BB-008** | Single rule producing one new state | One rule available on initial state, unavailable on result -> 2 states, 1 transition | Simplest non-trivial build |

---

## Category 3: Rule Availability Logic (RA)

| ID | Title | Description | Covers |
|---|---|---|---|
| **RA-001** | Rule never available | Single rule where `IsAvailable` always returns false -> 1 state, 0 transitions | Completely unavailable rule |
| **RA-002** | Rule available only on initial state | Rule checks `state.Variables["x"] == 0`, initial has x=0, execute sets x=1 -> 2 states, 1 transition | Conditional availability, single step |
| **RA-003** | Mix: one available, one unavailable | Two rules; first always available (sets x=1), second requires x=2 (never met) -> only first rule fires | Selective rule activation |
| **RA-004** | Rule becomes available after state change | Rules: R1 sets x=1, R2 requires x=1 and sets x=2 -> chain: S0->S1->S2 | Rule availability changes across states |
| **RA-005** | Rule availability depends on variable presence | Rule checks for key existence: `state.Variables.ContainsKey("flag")` -> only fires when variable exists | Key-existence-based conditions |
| **RA-006** | All rules available on every state | 3 rules, all always available, each produces unique states -> fan-out at every level | Maximum branching factor |

---

## Category 4: Cycle Detection (CD)

| ID | Title | Description | Covers |
|---|---|---|---|
| **CD-001** | Self-loop: rule clones input state | Rule returns `state.Clone()` -> 1 state, 1 transition where source==target | Simplest cycle: self-loop |
| **CD-002** | Two-state toggle | Initial x=false; rule toggles x -> states {false, true}, transitions S0->S1->S0 | Minimal multi-state cycle |
| **CD-003** | Three-state cycle | x cycles through 0->1->2->0 -> 3 states, 3 transitions forming a ring | Longer cycle |
| **CD-004** | Diamond convergence | From S0, rule A sets x=1 and rule B sets y=1; from both, a rule sets x=1,y=1 -> 4 states, two paths merge into S3 | Two independent paths to equivalent state |
| **CD-005** | Cycle back to initial state | Rule transforms state through several steps then back to initial -> transition back to "S0" | Cycle specifically targeting the start |
| **CD-006** | Self-loop does not block other rules | Rule A clones (self-loop), Rule B produces new state -> both transitions recorded, exploration continues | Cycle on one rule doesn't skip others |
| **CD-007** | Revisited state still gets transitions recorded | State S2 is reachable from S0 and from S1 -> two transitions pointing to S2 | Fan-in transitions to existing states |
| **CD-008** | No duplicate states in result | Toggle cycle explored fully -> `States.Count == 2`, not more despite revisits | Visited set prevents duplicates |

---

## Category 5: Exploration Strategy (ES)

| ID | Title | Description | Covers |
|---|---|---|---|
| **ES-001** | Default strategy is BFS | Default `BuilderConfig` -> state discovery order matches BFS (level-by-level) | Default enum value |
| **ES-002** | BFS: level-order state IDs | Tree: S0 branches to A,B; A branches to C,D -> BFS IDs: S0, S1(A), S2(B), S3(C), S4(D) | BFS discovery order |
| **ES-003** | DFS: depth-first state IDs | Same tree as ES-002 -> DFS IDs: S0, S1(B), S2(B's child), ... or S0, S1(A), S2(C)... depending on rule order | DFS discovery order |
| **ES-004** | BFS and DFS produce same state set | Same rules + same limits -> `States` dictionaries have same state values, just different IDs | Strategy doesn't change reachability |
| **ES-005** | Strategy affects which states survive MaxStates | Tree with branching, MaxStates=4 -> BFS picks breadth-first states, DFS picks depth-first states, different state sets | Strategy + limit interaction |

---

## Category 6: MaxDepth Limiting (DL)

| ID | Title | Description | Covers |
|---|---|---|---|
| **DL-001** | MaxDepth null: no depth limit | Finite chain of 5 states, MaxDepth=null -> all 5 states discovered | Null means unlimited |
| **DL-002** | MaxDepth=0: only initial state, not explored | Long chain, MaxDepth=0 -> 1 state, 0 transitions | Boundary: zero depth |
| **DL-003** | MaxDepth=1: initial explored, children not | Chain, MaxDepth=1 -> initial state + its direct children, but children not explored further | Single level of exploration |
| **DL-004** | MaxDepth=2: two levels explored | Linear chain with 5+ potential states, MaxDepth=2 -> 3 states (depths 0,1,2), only depths 0 and 1 explored | Two levels deep |
| **DL-005** | States at depth boundary are in machine but unexplored | MaxDepth=1, initial produces child -> child is in `States` and has incoming transition, but no outgoing transitions | Depth-boundary states exist but don't expand |
| **DL-006** | MaxDepth prevents infinite cycle | Rule always available (appends to list), MaxDepth=3 -> finite machine | Depth limit as termination guard |
| **DL-007** | MaxDepth with cycle at boundary | Toggle cycle, MaxDepth=1 -> S0 explored, S1 added but not explored, no back-transition S1->S0 | Cycle detection interacts with depth cutoff |

---

## Category 7: MaxStates Limiting (SL)

| ID | Title | Description | Covers |
|---|---|---|---|
| **SL-001** | MaxStates null: no state limit | Finite chain of 5 states, MaxStates=null -> all 5 states discovered | Null means unlimited |
| **SL-002** | MaxStates=1: only initial state | Rules produce new states but MaxStates=1 -> 1 state, 0 transitions to new states | Minimum state budget |
| **SL-003** | MaxStates=1 still allows self-loop transitions | Self-loop rule + MaxStates=1 -> 1 state, 1 transition (self-loop) | MaxStates check is after visited check |
| **SL-004** | MaxStates=3 stops at exactly 3 states | Chain of 10 potential states, MaxStates=3 -> exactly 3 states | Precise cutoff |
| **SL-005** | MaxStates breaks inner rule loop | 3 rules on S0, MaxStates=2 -> first new-state rule fires (S1 added), second new-state rule triggers break | Break exits rule loop, not outer loop |
| **SL-006** | Frontier states still explored after MaxStates hit | S0 produces S1 and S2 via two rules; MaxStates=3; S1 and S2 are on frontier -> their transitions to existing states are still recorded | Already-queued states continue processing |
| **SL-007** | MaxStates + MaxDepth both active | MaxDepth=2 and MaxStates=3, tree would produce 7 states -> both limits constrain | Combined limit interaction |

---

## Category 8: Transition Correctness (TC)

| ID | Title | Description | Covers |
|---|---|---|---|
| **TC-001** | Transition source/target IDs are valid state IDs | Every transition's SourceStateId and TargetStateId exist in `States` dictionary | Referential integrity |
| **TC-002** | Transition RuleName is rule's class name | Custom rule class `IncrementRule` -> transition.RuleName == `"IncrementRule"` | Rule name extraction from `GetType().Name` |
| **TC-003** | Multiple transitions from same source | S0 has 3 available rules -> 3 transitions with SourceStateId=="S0" | Fan-out transitions |
| **TC-004** | Multiple transitions to same target | Two paths to equivalent state -> two transitions with different sources but same target | Fan-in transitions |
| **TC-005** | Self-loop transition structure | Rule clones state -> transition with SourceStateId == TargetStateId == "S0" | Self-referential transition |
| **TC-006** | Transition count matches expected | Linear chain of N states -> exactly N-1 transitions | No extra/missing transitions |
| **TC-007** | Two rules producing identical new state | Rule A and Rule B both set x=1 from x=0 -> one state S1, two transitions S0->S1 with different RuleNames | Same target, different rules |
| **TC-008** | Transitions ordered by discovery | Rules evaluated in array order -> transitions appear in that evaluation order | Ordering guarantee |

---

## Category 9: Complex State Spaces (CS)

| ID | Title | Description | Covers |
|---|---|---|---|
| **CS-001** | Binary counter: 2 variables | Variables x,y each toggle 0/1 independently -> 4 states (00,01,10,11), multiple transitions | Multi-variable combinatorial explosion |
| **CS-002** | Conditional chain with branching | S0{x=0} -> R1 sets x=1 -> R2 (requires x=1) sets y=1 -> R3 (requires y=1) sets x=0 -> converges | Multi-step conditional chain |
| **CS-003** | Wide fan-out: 10 rules from initial | 10 distinct rules each producing unique state from S0 -> 11 states, 10 transitions from S0 | Large branching factor |
| **CS-004** | Deep chain bounded by MaxDepth | Rule increments counter, MaxDepth=100 -> exactly 101 states in a line | Deep linear exploration |
| **CS-005** | Rules that add new variables | Initial has {x:0}; Rule adds key "y" -> new state has {x:0, y:1} | Variable set grows across transitions |
| **CS-006** | Rules that remove variables | Initial has {x:0, y:0}; Rule removes "y" -> states have different key sets | Variable set shrinks (clone + remove) |
| **CS-007** | Mixed variable types | State has int, string, bool, null values; rules modify different typed variables | Equality/hashing across types |
| **CS-008** | State equality with null values | Rule sets variable to null; another state also has null for same key -> detected as equal | Null variable equality |

---

## Category 10: Edge Cases and Boundary Conditions (EC)

| ID | Title | Description | Covers |
|---|---|---|---|
| **EC-001** | MaxDepth=0 with self-loop rule | Self-loop rule exists but MaxDepth=0 -> 1 state, 0 transitions (initial never explored) | Depth zero suppresses even self-loops |
| **EC-002** | MaxStates=0 still includes initial state | Initial state is added before the loop; MaxStates=0 -> 1 state in machine | Initial state added unconditionally |
| **EC-003** | Single rule array with one element | `rules = [singleRule]` -> no index-out-of-bounds, processes correctly | Array boundary |
| **EC-004** | Large rules array (100 rules), only 1 available | 99 unavailable rules + 1 available -> correct single transition | Sparse rule availability |
| **EC-005** | Rule returns reference to input state (not clone) | Rule returns same `State` object -> `visited.Contains` finds it, self-loop recorded | Reference vs. value equality |
| **EC-006** | State with single variable set to null | `initialState.Variables["x"] = null` -> builds, state with null value preserved | Null in variable values |
| **EC-007** | State with many variables (50+) | Stress: state with 50 variables -> hashing, equality, clone all work | Scalability of state operations |
| **EC-008** | Two rules, same class, different instances | Two instances of same `TestRule` class with different lambdas -> same RuleName on transitions | RuleName collision from identical class names |
| **EC-009** | DFS with MaxStates favors deep states | Tree shape, DFS + MaxStates=4 -> deeper states discovered before shallow siblings | DFS bias under state limit |
| **EC-010** | BFS with MaxDepth vs MaxStates race | MaxDepth=3 and MaxStates=5, tree produces more than 5 at depth 3 -> whichever limit hits first wins | Limit precedence |

---

## Summary

| Category | IDs | Count | Focus |
|---|---|---|---|
| Input Validation | IV-001 to IV-007 | 7 | Null guards, error messages |
| Basic Build Behavior | BB-001 to BB-008 | 8 | Minimal valid builds, output structure |
| Rule Availability Logic | RA-001 to RA-006 | 6 | Conditional rule firing |
| Cycle Detection | CD-001 to CD-008 | 8 | Self-loops, toggles, diamonds, revisits |
| Exploration Strategy | ES-001 to ES-005 | 5 | BFS vs DFS ordering and interaction |
| MaxDepth Limiting | DL-001 to DL-007 | 7 | Depth boundary behavior |
| MaxStates Limiting | SL-001 to SL-007 | 7 | State budget enforcement |
| Transition Correctness | TC-001 to TC-008 | 8 | Structural integrity of transitions |
| Complex State Spaces | CS-001 to CS-008 | 8 | Real-world-like topologies |
| Edge/Boundary Conditions | EC-001 to EC-010 | 10 | Corner cases and limits |
| **Total** | | **74** | |

## Key Design Rationale

Each test case targets a specific behavioral facet of `StateMachineBuilder.cs`:

1. **Lines 7-15 (Validation)** - IV cases ensure every null path is exercised, including per-element index reporting.
2. **Lines 17-26 (Initialization)** - BB cases verify the initial state is always added as "S0" before exploration begins -- this is why EC-002 (MaxStates=0) still produces 1 state.
3. **Line 38 (MaxDepth gate)** - DL cases probe the `>=` comparison: a state at `depth == MaxDepth` is *in the machine* (added when discovered) but *not explored* (skipped by `continue`).
4. **Lines 48-51 (Cycle detection)** - CD cases verify that `visited.Contains()` relies on `State.Equals/GetHashCode` (value equality), not reference equality.
5. **Lines 55-56 (MaxStates break)** - SL cases verify the `break` exits the *inner rule loop*, not the outer frontier loop -- meaning already-queued states continue processing.
6. **Line 46 (`GetType().Name`)** - TC-002 and EC-008 verify rule naming, including the collision case when two rule instances share a class name.
