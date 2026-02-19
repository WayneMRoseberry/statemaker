## Relevant Files

- `src/StateMaker/State.cs` - Core State class with Dictionary<string, object> variables, Clone(), equality, and hashing
- `src/StateMaker.Tests/StateTests.cs` - Unit tests for State equality, hashing, Clone, and variable types
- `src/StateMaker/IRule.cs` - IRule interface defining IsAvailable and Execute methods
- `src/StateMaker/IRule.test.cs` - Unit tests for IRule contract verification
- `src/StateMaker/StateMachine.cs` - StateMachine class with IReadOnlyDictionary States, AddState/RemoveState methods, StartingStateId validation, and Transitions list
- `src/StateMaker/Transition.cs` - Transition class with SourceStateId, TargetStateId, and RuleName
- `src/StateMaker/StateDoesNotExistException.cs` - Custom exception thrown when StartingStateId references a non-existent state
- `src/StateMaker.Tests/StateMachineTests.cs` - Unit tests for StateMachine: AddState, RemoveState, StartingStateId validation, States read-only
- `src/StateMaker.Tests/TransitionTests.cs` - Unit tests for Transition properties and construction
- `src/StateMaker/BuilderConfig.cs` - BuilderConfig class with MaxDepth, MaxStates, ExplorationStrategy (BREADTHFIRSTSEARCH/DEPTHFIRSTSEARCH), LogLevel
- `src/StateMaker.Tests/BuilderConfigTests.cs` - Unit tests for BuilderConfig defaults and validation
- `src/StateMaker/IStateMachineBuilder.cs` - IStateMachineBuilder interface
- `src/StateMaker/StateMachineBuilder.cs` - StateMachineBuilder implementing BFS/DFS exploration, cycle detection, limits
- `src/StateMaker/StateMachineBuilder.test.cs` - Unit tests for builder: exploration, cycles, limits, validation
- `src/StateMaker/IExpressionEvaluator.cs` - IExpressionEvaluator interface with EvaluateBoolean and Evaluate methods
- `src/StateMaker/ExpressionEvaluator.cs` - Expression evaluator implementation using NCalc or DynamicExpresso
- `src/StateMaker/ExpressionEvaluator.test.cs` - Unit tests for expression evaluation: operators, variables, errors
- `src/StateMaker/DeclarativeRule.cs` - DeclarativeRule implementing IRule using expression evaluation
- `src/StateMaker/DeclarativeRule.test.cs` - Unit tests for declarative rule conditions and transformations
- `src/StateMaker/RuleFileLoader.cs` - JSON file loader for rule definitions and initial state
- `src/StateMaker/RuleFileLoader.test.cs` - Unit tests for file loading, validation, error handling
- `src/StateMaker/IStateMachineExporter.cs` - IStateMachineExporter interface
- `src/StateMaker/JsonExporter.cs` - JSON export implementation
- `src/StateMaker/JsonImporter.cs` - JSON import implementation
- `src/StateMaker/DotExporter.cs` - DOT/Graphviz export implementation
- `src/StateMaker/GraphMlExporter.cs` - GraphML/yEd export implementation
- `src/StateMaker/Exporters.test.cs` - Unit tests for all export/import formats and round-trip
- `src/StateMaker/ILogger.cs` - ILogger interface with log level support
- `src/StateMaker/ConsoleLogger.cs` - Default console logger implementation
- `src/StateMaker/Logger.test.cs` - Unit tests for logging at all levels
- `.github/workflows/ci.yml` - GitHub Actions CI pipeline with tests, coverage, and static analysis
- `src/StateMaker/StateMaker.csproj` - Project file with .NET 6.0+ target and dependencies

### Notes

- Unit tests should be placed in a separate test project (e.g., `src/StateMaker.Tests/`) following .NET conventions, or alongside source files if preferred.
- Use `dotnet test` to run tests. Use `dotnet test --collect:"XPlat Code Coverage"` for coverage with Coverlet.
- The project uses C#/.NET 6.0+ with the `StateMaker` namespace.

## Instructions for Completing Tasks

**IMPORTANT:** As you complete each task, you must check it off in this markdown file by changing `- [ ]` to `- [x]`. This helps track progress and ensures you don't skip any steps.

Example:
- `- [ ] 1.1 Read file` → `- [x] 1.1 Read file` (after completing)

Update the file after completing each sub-task, not just after completing an entire parent task.

**TDD Approach:** This project follows Test-Driven Development. When starting any new task or behavior change, follow the Red-Green-Refactor cycle:

1. **Red (failing test):** Write the unit tests first, then create a **stub implementation** of the method under test (e.g., returning a default value, throwing `NotImplementedException`, or providing an obviously incomplete body). Run the tests and confirm they **fail due to insufficient functionality** — not merely due to a missing method or compilation error. This step validates that the tests are actually verifying meaningful behavior.
2. **Green (make it pass):** Write the minimum correct implementation to make all tests pass.
3. **Refactor:** Clean up the implementation while keeping all tests green.

All tests must pass before moving on to the next sub-task.

## Tasks

- [x] 1.0 Set up CI/CD pipeline with GitHub Actions
  - [x] 1.1 Create `.github/workflows/ci.yml` with trigger on pull requests and pushes to main
  - [x] 1.2 Configure workflow to restore, build, and run `dotnet test` for all test projects
  - [x] 1.3 Add Coverlet for code coverage collection (`dotnet test --collect:"XPlat Code Coverage"`)
  - [x] 1.4 Configure 80% minimum coverage threshold and fail the build if not met
  - [x] 1.5 Add Roslyn analyzers for static code analysis
  - [x] 1.6 Configure GitHub branch protection rules to require passing CI checks before merge

- [x] 2.0 Set up project structure and core data structures (State, IRule, StateMachine, BuilderConfig, Transition)
  - [x] 2.1 Create solution file and `src/StateMaker/StateMaker.csproj` targeting .NET 6.0+ with `StateMaker` namespace
  - [x] 2.2 Create test project `src/StateMaker.Tests/StateMaker.Tests.csproj` referencing the main project
  - [x] 2.3 Implement `State` class with `Dictionary<string, object>` Variables property (primitives only: string, int, bool, float/double)
  - [x] 2.4 Implement `State.Clone()` method that creates a deep copy of the state
  - [x] 2.5 Implement `IEquatable<State>` on State: `Equals()` compares all variable keys and values, `GetHashCode()` uses sorted keys for deterministic hashing
  - [x] 2.6 Write unit tests for State: equality, hashing, Clone immutability, variable types, edge cases (empty state, null values)
  - [x] 2.7 Define `IRule` interface with `bool IsAvailable(State state)` and `State Execute(State state)` methods
  - [x] 2.8 Implement `Transition` class with `SourceStateId`, `TargetStateId`, and `RuleName` properties
  - [x] 2.9 Implement `StateMachine` class with `Dictionary<string, State> States`, `string StartingStateId`, and `List<Transition> Transitions`
  - [x] 2.10 Implement `BuilderConfig` class with `MaxDepth` (int?), `MaxStates` (int?), `ExplorationStrategy` (enum: BFS, DFS), and `LogLevel` (enum: INFO, DEBUG, ERROR)
  - [x] 2.11 Write unit tests for BuilderConfig default values and Transition properties

- [x] 3.0 Implement StateMachineBuilder with BFS/DFS exploration and cycle detection
  - [x] 3.1 Define `IStateMachineBuilder` interface with `StateMachine Build(State initialState, IRule[] rules, BuilderConfig config)` method
  - [x] 3.2 Implement `StateMachineBuilder` class with BFS exploration: use a queue of states, apply all available rules to each state
  - [x] 3.3 Implement cycle detection using `HashSet<State>` — skip exploration when an equivalent state already exists, but still record the transition
  - [x] 3.4 Implement sequential state ID generation (S0, S1, S2, ...)
  - [x] 3.5 Implement `MaxDepth` limit: track depth per state and stop exploring paths beyond the configured depth
  - [x] 3.6 Implement `MaxStates` limit: stop adding new states once the limit is reached
  - [x] 3.7 Implement DFS exploration strategy as an alternative to BFS (selected via `BuilderConfig.ExplorationStrategy`)
  - [x] 3.8 Derive rule names automatically from the rule class name (or allow configurable names)
  - [x] 3.9 Write unit tests: simple linear state chain, branching states, cycle detection, depth limit respected, state count limit respected, BFS vs DFS ordering
  - [x] 3.10 Write unit tests: no rules available (returns single-state machine), all rules always available, rules that produce duplicate states
  - [x] 3.11 Add `string GetName()` method to `IRule` interface (default returns `GetType().Name`), update `StateMachineBuilder` to call `rule.GetName()` instead of `rule.GetType().Name` when assigning `Transition.RuleName`, update `DeclarativeRule` to return its configured name from `GetName()`, and write unit tests verifying default and custom name behavior for both custom and declarative rules
  - [x] 3.12 Write tests with rules designed to produce specific state machine shapes: single state (no transitions fire), chains of varying length, and simple cycles
    - [x] 3.12.1 Create `src/StateMaker.Tests/StateMachineShapeTests.cs` test class with helper rule classes for shape testing (e.g., incrementing int rule, cycling modular arithmetic rule, always-false rule, clone-state rule)
    - [x] 3.12.2 Implement `AssertChainShape` helper method that validates chain topology: state count == chainLength + 1, transition count == chainLength, each non-last state has exactly one outgoing transition, each non-first state has exactly one incoming transition, `IsValidMachine()` returns true
    - [x] 3.12.3 Implement `AssertCycleShape` helper method that validates cycle topology: state count == cycleLength, transition count == cycleLength, exactly one back-edge to starting state, every state has outDegree == 1 and inDegree == 1, `IsValidMachine()` returns true
    - [x] 3.12.4 Write parameterized single-state shape tests: no rules (empty array), one rule where `IsAvailable` returns false, multiple rules where `IsAvailable` returns false, one rule where `Execute` returns clone of input (self-loop), initial state with various variable types (string, int, bool, float), initial state with no variables (empty state)
    - [x] 3.12.5 Write parameterized chain shape tests using `[Theory]` with varying lengths (1, 2, 3, 5, 10): chain built by incrementing an integer variable with `IsAvailable` guard (`step < N`), chain built by appending/cycling string variable values
    - [x] 3.12.6 Write parameterized simple cycle shape tests using `[Theory]` with varying lengths (2, 3, 5, 10): cycle built by modular arithmetic (`value = (value + 1) % N`), cycle built by cycling through a known list of string values
    - [x] 3.12.7 Verify all new shape tests pass alongside existing tests, ensure at least 5 distinct variations per shape category (single state, chain, cycle)
  - [x] 3.13 Write tests with rules designed to produce complex cycle shapes: cycles of varying depth and start points, cycles within cycles, and cycles with optional exits
    - [x] 3.13.1 Add helper rule classes for complex cycles: `ChainThenCycleRule` (increments through chain phase then cycles via modular arithmetic on a subset of values), `ConditionalBranchRule` (fires only when a variable has a specific value, producing an exit path)
    - [x] 3.13.2 Implement `AssertChainThenCycleShape` helper method: validates total state count == chainLength + cycleLength, transition count == chainLength + cycleLength, initial state has inDegree == 0, exactly one state has inDegree == 2 (cycle entry), no transition targets the initial state, `IsValidMachine()` returns true
    - [x] 3.13.3 Implement `AssertAllStatesReachable` helper method: BFS/DFS traversal from `StartingStateId` verifying every state in the machine is reachable
    - [x] 3.13.4 Write parameterized chain-then-cycle tests using `[Theory]` with varying chain/cycle length combinations: (1,2), (2,2), (3,3), (5,2), (1,5) — verify back-edge targets cycle entry state, not S0
    - [x] 3.13.5 Write parameterized cycle-start-point tests: verify which state the back-edge targets for cycles starting at S1 (chain=1), S2 (chain=2), S3 (chain=3) — assert back-edge `TargetStateId != StartingStateId` and target has inDegree == 2
    - [x] 3.13.6 Write nested cycle (cycles within cycles) tests: two adjacent cycles sharing a common state, sequential cycles (chain to cycle A with exit to cycle B), two independent cycles from a branch point, outer cycle length 3 with inner cycle length 2, outer cycle length 2 with inner cycle length 3
    - [x] 3.13.7 Write cycle-with-optional-exits tests: cycle of length 2 with one exit branch, cycle of length 3 with one exit branch, cycle of length 3 with exit chain of length 3, cycle of length 2 with exits from every cycle state, cycle of length 3 with two exits from same state
    - [x] 3.13.8 Verify all new complex cycle tests pass alongside existing 112+ tests, ensure at least 3 distinct variations per complex cycle category
  - [x] 3.14 Write tests with rules designed to produce branching shapes: varying peer count, depth, breadth, sub-branches as trees, connected sub-branches, and fully connected branches
    - [x] 3.14.1 Add helper rule classes for branching: multi-variable branch rules using `FuncRule` compositions that set different variable values to create distinct child states from the same parent
    - [x] 3.14.2 Implement `AssertTreeShape` helper method that validates tree topology: expected state count, expected transition count, all states reachable, `IsValidMachine()` returns true
    - [x] 3.14.3 Implement `AssertNoCycles` helper method that validates no back-edges exist in the graph (no transition targets an ancestor in any path from root)
    - [x] 3.14.4 Write parameterized varying peer count tests using `[Theory]` with fan-out values (2, 3, 5, 10): root branches to N terminal children, verify root outDegree == N, all children outDegree == 0 and inDegree == 1
    - [x] 3.14.5 Write parameterized depth tests for complete binary trees using `[Theory]` with depths (1, 2, 3): verify state count == 2^(depth+1) - 1, transition count == 2^(depth+1) - 2, tree structure with no convergence
    - [x] 3.14.6 Write parameterized breadth tests for trees with varying fan-out per level: (breadth=3, depth=2) producing 1+3+9=13 states, (breadth=2, depth=3) producing 15 states, (breadth=4, depth=2) producing 21 states
    - [x] 3.14.7 Write sub-branches as trees tests: root branches to independent sub-chains of varying lengths (2 chains of length 2, 2 chains of lengths 2 and 3, 3 chains of length 1, 2 binary sub-trees of depth 1)
    - [x] 3.14.8 Write connected sub-branches tests: two branches producing the same child state via deduplication, three branches where two produce the same child, root branches to children that each produce the same grandchild
    - [x] 3.14.9 Write fully connected branches tests: 2×2 level connectivity (5 states, 6 transitions), 2×3 connectivity (6 states, 8 transitions), 3×2 connectivity (6 states, 9 transitions)
    - [x] 3.14.10 Verify all new branching shape tests pass alongside existing tests, ensure at least 3 distinct variations per branching category
  - [x] 3.15 Write tests with rules designed to produce reconnecting branches (diamond/converging paths) and fully connected graphs with varying node counts
    - [x] 3.15.1 Implement `AssertDiamondShape` helper method that validates diamond topology: convergence point has inDegree == branchCount, correct state/transition counts, all states reachable, `IsValidMachine()` returns true
    - [x] 3.15.2 Implement `AssertFullyConnected` helper method that validates complete graph: K states, K*(K-1) transitions, every state has outDegree == K-1 and inDegree == K-1
    - [x] 3.15.3 Write simple diamond tests: classic 2-branch diamond (4 states, 4 transitions), diamond with chain prefix (5 states, 5 transitions), diamond with chain suffix (5 states, 5 transitions), deep diamond with 2-step branches before converging (6 states, 6 transitions)
    - [x] 3.15.4 Write parameterized wide convergence tests using `[Theory]` with branch counts (3, 4, 5): N branches from root all converge to same descendant, verify convergence point inDegree == N
    - [x] 3.15.5 Write stacked diamond tests: two sequential diamonds (7 states, 8 transitions), three sequential diamonds (10 states, 12 transitions), stacked with mixed branch counts (2-way then 3-way)
    - [x] 3.15.6 Write nested diamond tests: one branch is a sub-diamond converging at the outer convergence point, both branches contain sub-diamonds converging at the same final state
    - [x] 3.15.7 Write parameterized fully connected graph tests using `[Theory]` with node counts (2, 3, 4, 5): verify K states, K*(K-1) transitions, every state has outDegree == inDegree == K-1
    - [x] 3.15.8 Verify all new reconnecting branch and fully connected graph tests pass alongside existing 150+ tests, ensure at least 3 distinct variations per category
  - [x] 3.16 Write tests with rules designed to produce hybrid shapes combining multiple topologies (chains + cycles, branches + cycles, multiple shape neighborhoods)
    - [x] 3.16.1 Write chain + cycle hybrid tests: chain that branches to a terminal path and a cycle, two independent chain-then-cycle segments from a common root
    - [x] 3.16.2 Write branch + cycle hybrid tests: root branches to a terminal chain and a cycle, root branches to two independent cycles of different lengths, diamond convergence point entering a cycle
    - [x] 3.16.3 Write multiple shape neighborhood tests: chain -> branch -> cycle (three-phase topology), diamond with one cyclic branch and one chain branch, fully connected sub-graph reachable from a chain prefix
    - [x] 3.16.4 Write complex hybrid composition tests: branch where each arm has a different topology (chain, cycle, diamond), chain -> diamond -> cycle -> terminal, nested outer cycle with inner branch containing a sub-cycle
    - [x] 3.16.5 Verify all new hybrid shape tests pass alongside existing 167+ tests, ensure at least 3 distinct variations per hybrid category
  - [x] 3.17 Write tests verifying exploration strategy equivalence: same initial state, rules, and config must produce the same state machine (same states and transitions) under both BFS and DFS
    - [x] 3.17.1 Implement `AssertStrategyEquivalence` helper method that builds with both BFS and DFS configs and compares structural equivalence: same state count, same set of state variable dictionaries, same transition count, same set of (sourceVars, targetVars, ruleName) triples
    - [x] 3.17.2 Write simple shape equivalence tests: single state (no rules), chain of length 5, cycle of length 4, self-loop
    - [x] 3.17.3 Write branching shape equivalence tests: binary tree depth 2, fan-out of 4, connected sub-branches with deduplication
    - [x] 3.17.4 Write complex shape equivalence tests: chain-then-cycle (3,3), diamond (2-branch), nested cycles (outer 3 inner 2), cycle with exit chain
    - [x] 3.17.5 Write hybrid shape equivalence tests: branch to chain and cycle, diamond then cycle, three-phase chain-branch-cycle, branch with mixed topology arms
    - [x] 3.17.6 Verify all new equivalence tests pass alongside existing 182+ tests
  - [x] 3.18 Write tests for rule behavior edge cases: rules that always generate unique states (unbounded growth), rules that return malformed or unexpected states
  - [x] 3.19 Write tests for rule behavior edge cases: rules whose `IsAvailable` or `Execute` methods throw exceptions, and rules whose methods hang or take excessively long
  - [x] 3.20 Write tests for rule behavior edge cases: rules that mutate the input state passed to `Execute` (violating immutability), verifying the builder handles or detects this
    - [x] 3.20.1 Write test for rule that mutates input state variable directly (no clone): verify builder still completes and produces a valid machine
    - [x] 3.20.2 Write test for rule that adds a new variable to the input state: verify builder handles the mutation
    - [x] 3.20.3 Write test for rule that mutates input and returns it as the "new" state: verify builder produces expected output or detects the violation
    - [x] 3.20.4 Write test verifying the initial state object is not corrupted after building with a mutating rule
  - [x] 3.21 Write tests for resilience: null rules array, null elements within rules array, null config, and building from configurations with contradictory or nonsensical limits (e.g., MaxStates=0, MaxDepth=-1)
    - [x] 3.21.1 Write tests for MaxStates boundary values: MaxStates=0 (initial state still added), MaxStates=1 (exactly one state), MaxStates=-1 (negative value)
    - [x] 3.21.2 Write tests for MaxDepth boundary values: MaxDepth=0 (no exploration beyond initial), MaxDepth=-1 (negative value), MaxDepth=1 with branching rules
    - [x] 3.21.3 Write tests for combined limits: both MaxStates=1 and MaxDepth=1, MaxStates=2 with MaxDepth=0
    - [x] 3.21.4 Write tests for empty/minimal inputs: empty initial state (no variables), empty rules array with limits set, single rule that is never available
    - [x] 3.21.5 Verify all new edge case and resilience tests pass alongside existing 197+ tests
  - [x] 3.22 Implement a test case generator tool that programmatically combines initial state shapes (no variables, one of each data type, multiple variables, 1..N), config combinations (MaxStates x MaxDepth pairwise from null/0/-1/1/2/3/10), and rule variations (sets variable, adds variable, increments, empty) to produce build definition files
    - [x] 3.22.1 Create `BuildDefinition` record with Name, InitialState, Rules, and Config properties
    - [x] 3.22.2 Implement initial state shape generators: empty, single string/int/bool/double variable, multiple variables, N int variables (1..5)
    - [x] 3.22.3 Implement config combination generator with pairwise MaxStates x MaxDepth from null/0/-1/1/2/3/10 and both exploration strategies
    - [x] 3.22.4 Implement rule variation generators: empty array, sets variable, adds variable, increments int, multiple rules, never-available rule
    - [x] 3.22.5 Implement `GenerateAll()` method combining state shapes, configs, and rule variations into BuildDefinitions
  - [x] 3.23 Implement a test battery executor tool that runs a set of build definitions through the builder and applies oracle checks: no crash/exception, no infinite loop (heuristic timeout), MaxStates and MaxDepth limits respected in output
    - [x] 3.23.1 Create `TestBatteryResult` record with DefinitionName, Passed, FailureReason, StateCount, TransitionCount
    - [x] 3.23.2 Implement executor that runs each BuildDefinition through StateMachineBuilder.Build with timeout protection
    - [x] 3.23.3 Implement oracle check: MaxStates limit respected in output (States.Count <= MaxStates when positive)
    - [x] 3.23.4 Implement oracle check: MaxDepth limit respected via BFS path-length validation
    - [x] 3.23.5 Implement oracle check: IsValidMachine() returns true for all successful builds
    - [x] 3.23.6 Write xUnit tests: Theory with MemberData for individual build definition results, Fact for all-pass summary
    - [x] 3.23.7 Verify all tests pass alongside existing 212+ tests
  - [x] 3.24 Implement oracle checks in the test battery executor for performance validation: time-to-size ratio within expected bounds, and expected state machine shape matching for tractable cases
    - [x] 3.24.1 Add `ExpectedShapeInfo` record and extend `BuildDefinition` with optional `ExpectedShape` field
    - [x] 3.24.2 Add `ElapsedTime` to `TestBatteryResult` and capture build duration in executor
    - [x] 3.24.3 Implement time-to-size ratio oracle: elapsed ms per state below threshold (100ms/state)
    - [x] 3.24.4 Implement shape matching oracle: verify expected state count, transition count, and max depth when specified
    - [x] 3.24.5 Write tests verifying performance oracle passes for known-good definitions and shape oracle matches expected shapes
  - [x] 3.25 Implement a reverse rule generator tool that takes a target state machine shape as input and generates one or more sets of rules that would build it, including variations (extra non-triggering rules, different rule orderings) that should not alter the expected output
    - [x] 3.25.1 Implement chain shape generator: produces rules and initial state for chains of varying length with expected shape info
    - [x] 3.25.2 Implement cycle shape generator: produces rules for cycles of varying length
    - [x] 3.25.3 Implement chain-then-cycle, binary tree, diamond, and fully connected shape generators
    - [x] 3.25.4 Implement variation generators: add non-triggering rules, shuffle rule ordering
    - [x] 3.25.5 Write tests: generate reverse rules for each shape, run through battery executor, verify shape oracle passes
    - [x] 3.25.6 Verify all tests pass alongside existing 279+ tests

- [x] 4.0 Implement configuration validation
  - [x] 4.1 Add null check for initial state — throw `ArgumentNullException` (already implemented: `ArgumentNullException.ThrowIfNull`)
  - [x] 4.2 Validate exhaustive mode: both `MaxDepth` and `MaxStates` null is valid
  - [x] 4.3 Validate state-limited mode: `MaxStates` set with `MaxDepth` null is valid
  - [x] 4.4 Validate dual-limited mode: both `MaxDepth` and `MaxStates` set is valid
  - [x] 4.5 ~~Reject depth-only mode~~ — **Skipped**: depth-only mode is valid and used throughout the test suite (see PRD)
  - [x] 4.6 Write unit tests for each valid configuration combination confirming no exception is thrown
    - [x] 4.6.1 Write test for exhaustive mode (both null) with BFS and DFS — no exception, valid machine
    - [x] 4.6.2 Write test for state-limited mode (MaxStates set, MaxDepth null) with BFS and DFS
    - [x] 4.6.3 Write test for depth-limited mode (MaxDepth set, MaxStates null) with BFS and DFS
    - [x] 4.6.4 Write test for dual-limited mode (both set) with BFS and DFS
    - [x] 4.6.5 Write test for each valid config with empty rules and active rules
  - [x] 4.7 Write unit tests for each invalid configuration confirming the correct exception type and message
    - [x] 4.7.1 Write test for null initialState (already exists, verify coverage)
    - [x] 4.7.2 Write test for null rules array (already exists, verify coverage)
    - [x] 4.7.3 Write test for null config (already exists, verify coverage)
    - [x] 4.7.4 Write test for null element in rules array (already exists, verify coverage)

- [x] 5.0 Implement expression evaluation system
  - [x] 5.1 Define `IExpressionEvaluator` interface with `bool EvaluateBoolean(string expression, Dictionary<string, object> variables)` and `object Evaluate(string expression, Dictionary<string, object> variables)`
  - [x] 5.2 Choose and add expression evaluator library dependency (NCalcSync v5.11.0) to the project
  - [x] 5.3 Implement `ExpressionEvaluator` class supporting Phase 1 comparison operators: `==`, `!=`, `<`, `>`, `<=`, `>=`
  - [x] 5.4 Implement Phase 1 logical operators: `&&`, `||`, `!`
  - [x] 5.5 Implement literal value support: strings (`'Pending'`), integers, booleans (`true`/`false`), floats
  - [x] 5.6 Implement variable resolution: case-sensitive exact match against state variable dictionary keys
  - [x] 5.7 Implement Phase 2 arithmetic operators: `+`, `-`, `*`, `/` and parenthetical grouping
  - [x] 5.8 Implement error handling: undefined variable, type mismatch, division by zero, invalid syntax — each with clear error messages
  - [x] 5.9 Ensure expressions are sandboxed (no file system, network, reflection, or arbitrary code execution)
  - [x] 5.10 Write unit tests for each operator type, variable resolution with all primitive types, compound expressions, and all error cases

- [x] 6.0 Implement declarative rules and JSON file loader
  - [x] 6.1 Implement `DeclarativeRule` class that implements `IRule`, storing name, condition string, and transformations dictionary
  - [x] 6.2 Implement `DeclarativeRule.IsAvailable()` — evaluates condition expression against state variables using `IExpressionEvaluator.EvaluateBoolean()`
  - [x] 6.3 Implement `DeclarativeRule.Execute()` — clones state, evaluates each transformation expression against the **original** state variables, sets new values on the clone
  - [x] 6.4 Write unit tests for DeclarativeRule: condition evaluation, transformation application, immutability of input state, multiple transformations
  - [x] 6.5 Implement `RuleFileLoader` class that reads a JSON file and returns initial state (if present) and an `IRule[]` array
  - [x] 6.6 Implement JSON parsing for `initialState` object — construct a `State` from the key-value pairs
  - [x] 6.7 Implement JSON parsing for declarative rule entries (rules without `type` or with `type: "declarative"`) — create `DeclarativeRule` instances
  - [x] 6.8 Implement JSON parsing for custom rule entries (`type: "custom"`) — load assembly, instantiate class via reflection, validate it implements `IRule`
  - [x] 6.9 Implement validation and clear error messages: missing required fields, invalid JSON syntax, class not found, class doesn't implement IRule, no parameterless constructor
  - [x] 6.10 Write unit tests for file loader: valid declarative rules, valid custom rules, mixed rules, missing initialState, present initialState, all error cases
  - [x] 6.11 Provide a programmatic API method to create declarative rules without a file (e.g., `RuleBuilder.DefineRule(name, condition, transformations)`)
  - [x] 6.12 Write unit tests for programmatic declarative rule creation

- [x] 7.0 Implement export and import capabilities (JSON, DOT, GraphML)
  - [x] 7.1 Define `IStateMachineExporter` interface with `string Export(StateMachine stateMachine)` method
  - [x] 7.2 Define `IStateMachineImporter` interface with `StateMachine Import(string content)` method
  - [x] 7.3 Implement `JsonExporter`: serialize StateMachine to JSON with `startingStateId`, `states` (id-to-variables map), and `transitions` array
  - [x] 7.4 Implement `JsonImporter`: deserialize JSON back to a `StateMachine` object preserving all state IDs, variables, and transitions
  - [x] 7.5 Write unit tests for JSON round-trip: export then import produces an equivalent StateMachine
  - [x] 7.6 Implement `DotExporter`: generate DOT format with box nodes showing state ID + variables, directed edges with rule names, starting state indicator
  - [x] 7.7 Write unit tests for DOT export: valid DOT syntax, all states and transitions present, starting state arrow
  - [x] 7.8 Implement `GraphMlExporter`: generate GraphML/yEd-compatible XML with ShapeNode elements, edge labels, visual properties (colors, shapes)
  - [x] 7.9 Write unit tests for GraphML export: valid XML structure, all states and transitions present, yEd visual properties
  - [x] 7.10 Write unit tests for import-then-re-export: import from JSON, export to DOT and GraphML, verify no data loss

- [x] 8.0 Implement logging and diagnostics
  - [x] 8.1 Define `IStateMachineLogger` interface with methods for INFO, DEBUG, and ERROR log levels
  - [x] 8.2 Implement `ConsoleLogger` class that outputs to the console, respecting the configured `LogLevel`
  - [x] 8.3 Integrate logging into `StateMachineBuilder`: log state discovery, rule application, cycle detection, limit reached events
  - [x] 8.4 Default logging level is INFO and ERROR (DEBUG disabled unless configured)
  - [x] 8.5 Ensure the logging system is extensible — users can provide custom `ILogger` implementations
  - [x] 8.6 Write unit tests for logging: correct messages at each level, DEBUG suppressed by default, custom logger receives expected calls
