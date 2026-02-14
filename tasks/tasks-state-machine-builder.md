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
  - [ ] 3.11 Write tests with rules designed to produce specific state machine shapes: single state (no transitions fire), chains of varying length, and simple cycles
  - [ ] 3.12 Write tests with rules designed to produce complex cycle shapes: cycles of varying depth and start points, cycles within cycles, and cycles with optional exits
  - [ ] 3.13 Write tests with rules designed to produce branching shapes: varying peer count, depth, breadth, sub-branches as trees, connected sub-branches, and fully connected branches
  - [ ] 3.14 Write tests with rules designed to produce reconnecting branches (diamond/converging paths) and fully connected graphs with varying node counts
  - [ ] 3.15 Write tests with rules designed to produce hybrid shapes combining multiple topologies (chains + cycles, branches + cycles, multiple shape neighborhoods)
  - [ ] 3.16 Write tests verifying exploration strategy equivalence: same initial state, rules, and config must produce the same state machine (same states and transitions) under both BFS and DFS
  - [ ] 3.17 Write tests for rule behavior edge cases: rules that always generate unique states (unbounded growth), rules that return malformed or unexpected states
  - [ ] 3.18 Write tests for rule behavior edge cases: rules whose `IsAvailable` or `Execute` methods throw exceptions, and rules whose methods hang or take excessively long
  - [ ] 3.19 Write tests for rule behavior edge cases: rules that mutate the input state passed to `Execute` (violating immutability), verifying the builder handles or detects this
  - [ ] 3.20 Write tests for resilience: null rules array, null elements within rules array, null config, and building from configurations with contradictory or nonsensical limits (e.g., MaxStates=0, MaxDepth=-1)
  - [ ] 3.21 Implement a test case generator tool that programmatically combines initial state shapes (no variables, one of each data type, multiple variables, 1..N), config combinations (MaxStates x MaxDepth pairwise from null/0/-1/1/2/3/10), and rule variations (sets variable, adds variable, increments, empty) to produce build definition files
  - [ ] 3.22 Implement a test battery executor tool that runs a set of build definitions through the builder and applies oracle checks: no crash/exception, no infinite loop (heuristic timeout), MaxStates and MaxDepth limits respected in output
  - [ ] 3.23 Implement oracle checks in the test battery executor for performance validation: time-to-size ratio within expected bounds, and expected state machine shape matching for tractable cases
  - [ ] 3.24 Implement a reverse rule generator tool that takes a target state machine shape as input and generates one or more sets of rules that would build it, including variations (extra non-triggering rules, different rule orderings) that should not alter the expected output

- [ ] 4.0 Implement configuration validation
  - [ ] 4.1 Add null check for initial state — throw `ArgumentNullException` with message "No initial state provided"
  - [ ] 4.2 Validate exhaustive mode: both `MaxDepth` and `MaxStates` null is valid
  - [ ] 4.3 Validate state-limited mode: `MaxStates` set with `MaxDepth` null is valid
  - [ ] 4.4 Validate dual-limited mode: both `MaxDepth` and `MaxStates` set is valid
  - [ ] 4.5 Reject depth-only mode: `MaxDepth` set but `MaxStates` null — throw `InvalidOperationException` with specific message
  - [ ] 4.6 Write unit tests for each valid configuration combination confirming no exception is thrown
  - [ ] 4.7 Write unit tests for each invalid configuration confirming the correct exception type and message

- [ ] 5.0 Implement expression evaluation system
  - [ ] 5.1 Define `IExpressionEvaluator` interface with `bool EvaluateBoolean(string expression, Dictionary<string, object> variables)` and `object Evaluate(string expression, Dictionary<string, object> variables)`
  - [ ] 5.2 Choose and add expression evaluator library dependency (NCalc or DynamicExpresso) to the project
  - [ ] 5.3 Implement `ExpressionEvaluator` class supporting Phase 1 comparison operators: `==`, `!=`, `<`, `>`, `<=`, `>=`
  - [ ] 5.4 Implement Phase 1 logical operators: `&&`, `||`, `!`
  - [ ] 5.5 Implement literal value support: strings (`'Pending'`), integers, booleans (`true`/`false`), floats
  - [ ] 5.6 Implement variable resolution: case-sensitive exact match against state variable dictionary keys
  - [ ] 5.7 Implement Phase 2 arithmetic operators: `+`, `-`, `*`, `/` and parenthetical grouping
  - [ ] 5.8 Implement error handling: undefined variable, type mismatch, division by zero, invalid syntax — each with clear error messages
  - [ ] 5.9 Ensure expressions are sandboxed (no file system, network, reflection, or arbitrary code execution)
  - [ ] 5.10 Write unit tests for each operator type, variable resolution with all primitive types, compound expressions, and all error cases

- [ ] 6.0 Implement declarative rules and JSON file loader
  - [ ] 6.1 Implement `DeclarativeRule` class that implements `IRule`, storing name, condition string, and transformations dictionary
  - [ ] 6.2 Implement `DeclarativeRule.IsAvailable()` — evaluates condition expression against state variables using `IExpressionEvaluator.EvaluateBoolean()`
  - [ ] 6.3 Implement `DeclarativeRule.Execute()` — clones state, evaluates each transformation expression against the **original** state variables, sets new values on the clone
  - [ ] 6.4 Write unit tests for DeclarativeRule: condition evaluation, transformation application, immutability of input state, multiple transformations
  - [ ] 6.5 Implement `RuleFileLoader` class that reads a JSON file and returns initial state (if present) and an `IRule[]` array
  - [ ] 6.6 Implement JSON parsing for `initialState` object — construct a `State` from the key-value pairs
  - [ ] 6.7 Implement JSON parsing for declarative rule entries (rules without `type` or with `type: "declarative"`) — create `DeclarativeRule` instances
  - [ ] 6.8 Implement JSON parsing for custom rule entries (`type: "custom"`) — load assembly, instantiate class via reflection, validate it implements `IRule`
  - [ ] 6.9 Implement validation and clear error messages: missing required fields, invalid JSON syntax, class not found, class doesn't implement IRule, no parameterless constructor
  - [ ] 6.10 Write unit tests for file loader: valid declarative rules, valid custom rules, mixed rules, missing initialState, present initialState, all error cases
  - [ ] 6.11 Provide a programmatic API method to create declarative rules without a file (e.g., `RuleBuilder.DefineRule(name, condition, transformations)`)
  - [ ] 6.12 Write unit tests for programmatic declarative rule creation

- [ ] 7.0 Implement export and import capabilities (JSON, DOT, GraphML)
  - [ ] 7.1 Define `IStateMachineExporter` interface with `string Export(StateMachine stateMachine)` method
  - [ ] 7.2 Define `IStateMachineImporter` interface with `StateMachine Import(string content)` method
  - [ ] 7.3 Implement `JsonExporter`: serialize StateMachine to JSON with `startingStateId`, `states` (id-to-variables map), and `transitions` array
  - [ ] 7.4 Implement `JsonImporter`: deserialize JSON back to a `StateMachine` object preserving all state IDs, variables, and transitions
  - [ ] 7.5 Write unit tests for JSON round-trip: export then import produces an equivalent StateMachine
  - [ ] 7.6 Implement `DotExporter`: generate DOT format with box nodes showing state ID + variables, directed edges with rule names, starting state indicator
  - [ ] 7.7 Write unit tests for DOT export: valid DOT syntax, all states and transitions present, starting state arrow
  - [ ] 7.8 Implement `GraphMlExporter`: generate GraphML/yEd-compatible XML with ShapeNode elements, edge labels, visual properties (colors, shapes)
  - [ ] 7.9 Write unit tests for GraphML export: valid XML structure, all states and transitions present, yEd visual properties
  - [ ] 7.10 Write unit tests for import-then-re-export: import from JSON, export to DOT and GraphML, verify no data loss

- [ ] 8.0 Implement logging and diagnostics
  - [ ] 8.1 Define `ILogger` interface with methods for INFO, DEBUG, and ERROR log levels
  - [ ] 8.2 Implement `ConsoleLogger` class that outputs to the console, respecting the configured `LogLevel`
  - [ ] 8.3 Integrate logging into `StateMachineBuilder`: log state discovery, rule application, cycle detection, limit reached events
  - [ ] 8.4 Default logging level is INFO and ERROR (DEBUG disabled unless configured)
  - [ ] 8.5 Ensure the logging system is extensible — users can provide custom `ILogger` implementations
  - [ ] 8.6 Write unit tests for logging: correct messages at each level, DEBUG suppressed by default, custom logger receives expected calls
