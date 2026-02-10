# Product Requirements Document: State Machine Builder

## Introduction/Overview

The State Machine Builder is a C#/.NET library that generates states and transitions of a finite state machine from an initial state and a set of transformation rules. The tool explores the state space systematically according to configurable limits, creating a state machine definition that can be used for various purposes including software testing, workflow modeling, and system analysis.

Users configure the builder's exploration behavior through `BuilderConfig`:
- **Exhaustive exploration** (no limits) generates all reachable states
- **Limited exploration** (with depth/state count limits) generates a bounded subset of states

**Problem it solves:** Manually defining all possible states and transitions in a complex system is time-consuming, error-prone, and difficult to maintain. This tool automates the discovery of states by applying rules iteratively, to help establish completeness and correctness.

**Goal:** Provide a general-purpose, extensible library that automatically builds finite state machines from declarative rule definitions, with configurable exploration strategies and export capabilities.

## Goals

1. **Automated State Discovery:** Generate all reachable states from an initial state by applying a set of rules
2. **Cycle Prevention:** Detect and prevent infinite loops by recognizing previously encountered states
3. **Configurable Exploration:** Support depth and state count limits to handle large state spaces
4. **Extensibility:** Allow developers to define custom rules by implementing a simple interface
5. **Declarative Rule Definition:** Enable users to define rules without writing code, using boolean expressions and transformations
6. **File-Based Configuration:** Support loading rule definitions from external files for non-programmers
7. **Export Capabilities:** Enable export of generated state machines to standard formats (GraphML, DOT, JSON) for visualization and analysis
8. **Type Safety:** Leverage C#/.NET type system for compile-time safety and IDE support

## User Stories

### Story 1: Test Engineer
As a test engineer, I want to configure the state machine builder to automatically generate all reachable states of my application from a set of user actions, so that I can create comprehensive test coverage without manually enumerating every scenario.

### Story 2: Library User
As a developer using the library, I want to define custom rules by implementing a simple interface (isAvailable, execute), so that I can model my domain-specific state transitions without learning complex APIs.

### Story 3: System Analyst
As a system analyst, I want to export the generated state machine to GraphML or DOT format, so that I can visualize and analyze the system behavior using tools like yEd or Graphviz.

### Story 4: Developer
As a developer, I want to configure depth and state count limits, so that I can control the exploration process and prevent my application from running out of memory on large or potentially infinite state spaces.

### Story 5: Business Analyst
As a business analyst without programming expertise, I want to define state machine rules using a declarative format via file so that I can model workflows and processes without writing C# code.

### Story 6: Developer - Configuration Validation
As a developer using the library, I want the builder to validate my configuration settings and initial state before attempting to build the state machine, so that I receive clear error messages about invalid configurations rather than unexpected runtime behavior or infinite loops.

## User Stories in Gherkin Format

### Feature: Automated State Machine Generation

**Scenario 1: Test Engineer generates comprehensive test coverage**
```gherkin
Feature: Generate all possible application states for testing

Scenario: Generate test states from user actions
  Given I am a test engineer with a set of user action rules
  And I have defined an initial application state
  And I configure the builder for exhaustive exploration (no depth or state limits)
  When I build the state machine with these rules and configuration
  Then all reachable states are generated
  And I can create comprehensive test coverage without manual enumeration

  Scenario: tester builds full coverage from initial state and configuration
    Given Test engineer has a set of user action rules
    And defined initial state (YES)
    And configured exhaustive (YES)
    When build state machine
    Then all possible reachable states generated
    And I can create comprehensive coverage

  Scenario: tester builds coverage with depth and state limit
    Given Test engineer has a set of user action rules
    And defined initial state (YES)
    And configured exhaustive (NO)
    And configured depth limit? (YES)
    And configured state limit (YES)
    When build state machine
    Then possible states up to limit are generated.
    And I can create comprehensive coverage

  Scenario: tester builds coverage with depth limit
    Given Test engineer has a set of user action rules
    And defined initial state (YES)
    And configured exhaustive (NO)
    And configured depth limit? (YES)
    And configured state limit (NO)
    When build state machine
    Then an error saying invalid configuration state

  Scenario: tester builds coverage with state limit
    Given Test engineer has a set of user action rules
    And defined initial state (YES)
    And configured exhaustive (NO)
    And configured depth limit? (NO)
    And configured state limit (YES)
    When build state machine
    Then possible states up to limit are generated.
    And I can create comprehensive coverage

  Scenario: tester submits configuration with no setting
    Given Test engineer has a set of user action rules
    And defined initial state (YES)
    And configured exhaustive (NO)
    And configured depth limit? (NO)
    And configured state limit (NO)
    When build state machine
    Then an error saying invalid configuration state

  Scenario: tester builds without initial state
    Given Test engineer has a set of user action rules
    And defined initial state (NO)
    When build state machine
    Then an error is displayed saying no state presented
```

**Scenario 2: Developer implements custom domain rules**
```gherkin
Feature: Define custom state transition rules

Scenario: Implement domain-specific rules with simple interface
  Given I am a developer who needs to model domain-specific transitions
  And I have the IRule interface with IsAvailable and Execute methods
  When I implement the interface for my custom rule
  Then I can model my state transitions without learning complex APIs
  And the rule integrates seamlessly with the builder
```

**Scenario 3: System Analyst exports for visualization**
```gherkin
Feature: Export state machines to standard formats

Scenario: Export state machine to GraphML format
  Given I am a system analyst with a generated state machine
  When I export the state machine to GraphML format
  Then I receive a valid GraphML file
  And I can open and visualize it in yEd

Scenario: Export state machine to DOT format
  Given I am a system analyst with a generated state machine
  When I export the state machine to DOT format
  Then I receive a valid DOT file
  And I can visualize it using Graphviz
```

**Scenario 4: Developer configures exploration limits**
```gherkin
Feature: Control state space exploration

Scenario: Set depth limit to prevent deep exploration
  Given I am a developer working with a potentially large state space
  And I configure BuilderConfig with MaxDepth = 10
  When I build the state machine
  Then exploration stops at 10 levels deep
  And my application does not run out of memory

Scenario: Set state count limit to cap total states
  Given I am a developer working with a potentially infinite state space
  And I configure BuilderConfig with MaxStates = 1000
  When I build the state machine
  Then exploration stops after generating 1000 states
  And my application does not run out of memory
```

**Scenario 5: Business Analyst defines rules declaratively**
```gherkin
Feature: Define rules without writing code

Scenario: Define a rule using declarative API
  Given I am a business analyst without programming skills
  And I want to create a rule named "ApproveOrder"
  When I use the declarative API to define:
    | Property      | Value                           |
    | Conditions    | OrderStatus == "Pending"        |
    | Transformation| OrderStatus = "Approved"        |
  Then the rule is created and available for state machine building
  And I did not need to write any C# code

Scenario: Load rules from a definition file
  Given I am a business analyst with a rule definition file
  And the file contains multiple rule definitions with conditions and transformations
  When I load the definition file using the library
  Then all rules are parsed and created successfully
  And I can build a state machine using these rules
```

## Functional Requirements

### Core Data Structures

1. The system must provide a `State` class that stores a set of variables with their values
2. The `State` class must implement equality comparison to determine if two states are equivalent (same variables with same values)
3. The system must provide a `Rule` interface with two methods:
   - `bool IsAvailable(State state)` - returns true if the rule can be applied to the given state
   - `State Execute(State state)` - returns a new state resulting from applying the rule
4. The system must provide a `StateMachine` class with the following properties:
   - `Dictionary<string, State> States` - all discovered states, keyed by unique ID
   - `string StartingStateId` - the ID of the initial state
   - `List<Transition> Transitions` - list of state transitions with source ID, target ID, and rule name
5. The system must provide a `BuilderConfig` class for configuration settings

### State Machine Builder

6. The system must provide a `StateMachineBuilder` class implementing an `IStateMachineBuilder` interface
7. The builder must implement a `Build(State initialState, Rule[] rules, BuilderConfig config)` method that requires a configuration parameter
8. The builder must start from the initial state and iteratively apply all available rules
9. For each new state generated, the builder must check if an equivalent state already exists
10. If an equivalent state exists, the builder must create a transition to the existing state and **stop exploring that path** (cycle prevention)
11. If the state is new, the builder must add it to the state machine and continue exploration
12. The builder must respect the depth limit specified in `BuilderConfig.MaxDepth` if set (maximum levels of transitions from initial state)
13. The builder must respect the state count limit specified in `BuilderConfig.MaxStates` if set (maximum total states)
14. The builder must stop exploration when either limit is reached, if configured
15. If both `MaxDepth` and `MaxStates` are null (not set), the builder performs **exhaustive exploration** generating all reachable states
16. The builder must assign unique IDs to each state

### Configuration Validation

17. The builder must validate that the initial state parameter is not null before beginning exploration
18. If the initial state is null, the builder must throw an `ArgumentNullException` with the message "No initial state provided"
19. The builder must validate the configuration settings before beginning exploration
20. Valid configuration combinations are:
    - **Exhaustive mode:** Both `MaxDepth` and `MaxStates` are null
    - **State-limited mode:** `MaxStates` is set (with or without `MaxDepth`)
    - **Dual-limited mode:** Both `MaxDepth` and `MaxStates` are set
21. Invalid configuration combinations that must result in an error:
    - **Depth-only mode:** `MaxDepth` is set but `MaxStates` is null (invalid - requires state limit)
    - **No limits configured:** Both `MaxDepth` and `MaxStates` are null AND exhaustive mode is not explicitly enabled
22. When an invalid configuration is detected, the builder must throw an `InvalidOperationException` with the message "Invalid configuration: {specific reason}"
23. The error message must clearly indicate which configuration requirement was violated

### Export Capabilities

24. The system must provide an export mechanism to serialize the state machine to JSON format
25. The system must provide an export mechanism to generate GraphML format for tools like yEd
26. The system must provide an export mechanism to generate DOT format for Graphviz
27. Each export format must include all states, transitions, and rule names

### Namespace and Extensibility

28. All interfaces (`IRule`, `IStateMachineBuilder`) and core classes (`State`, `StateMachine`, `BuilderConfig`, `Transition`) must be in the `StateMaker` namespace
29. The namespace must be designed to allow external assemblies to reference it and implement custom `IRule` implementations
30. Rule names should be automatically derived from the rule class name (or configurable)

### Declarative Rule Definition

31. The system must provide a declarative rule definition mechanism that does not require writing custom C# classes
32. A declarative rule definition must include:
    - Rule name (string identifier)
    - Availability condition (boolean expression evaluated against state variables)
    - Variable transformations (mapping of variable names to new values or expressions)
33. The system must provide an API method to create declarative rules programmatically (e.g., `DefineRule(name, condition, transformations)`)
34. The system must support boolean expressions for conditions using standard operators (initial version):
    - Equality: `==`, `!=`
    - Comparison: `<`, `>`, `<=`, `>=`
    - Logical: `&&`, `||`, `!`
    - Example: `"Age >= 18 && Status == 'Active'"`
35. The system must support transformation expressions that can (initial version):
    - Set variables to literal values: `Status = "Approved"`
    - Reference current state variables: `Count = Count + 1`
    - Use basic arithmetic: `+`, `-`, `*`, `/`, and parenthetical expressions
36. The system must provide a file loader that reads rule definitions from an external file
37. The file format must be JSON only (structured and human-readable)
38. The file loader must validate rule definitions at execution time and provide clear error messages for invalid syntax
39. Declarative rules must implement the same `IRule` interface as code-based rules, ensuring they work identically in the builder
40. State variable references in expressions must be case-sensitive exact name matches
41. A declarative state machine definition must support mixed rules (both declarative and programmatically-defined rules)

### Logging and Diagnostics

42. The system must provide a logging mechanism with three severity levels:
    - INFO: General information about state machine building progress
    - DEBUG: Detailed information for in-depth investigation
    - ERROR: Error conditions
43. The default logging level must be INFO and ERROR (DEBUG disabled by default)
44. The logging system must support extensible loggers to allow custom destinations
45. The default logger must output to the console
46. State variables must support only primitive types in the initial version (strings, integers, booleans, floats)

## Non-Goals (Out of Scope)

The following are explicitly **not** included in the initial version (may be considered for future releases):

1. **Built-in Visualization UI:** No graphical rendering within the library itself (export formats allow external visualization)
2. **Rule Conflict Resolution:** No automatic handling of conflicting rules; developers must ensure rules are deterministic
3. **Rule Priority Mechanism:** All available rules are evaluated; no priority ordering (future consideration)
4. **Parallel Rule Application:** Rules are applied sequentially; no concurrent state generation
5. **State Machine Execution Engine:** The library builds state machines but does not execute or simulate them
6. **Undo/Redo of State Transitions:** The builder is a forward-only exploration
7. **Incremental Building:** Each `Build()` call starts fresh; no support for adding rules to an existing state machine
8. **Multi-language Support:** Initial version is C#/.NET only (may expand in future versions)
9. **Advanced Export Formats:** Formats like SCXML, Petri nets are not included initially
10. **YAML/XML File Formats:** JSON only for declarative rules; YAML and XML not supported
11. **Complex State Variable Types:** Only primitive types (strings, integers, booleans, floats) supported initially
12. **Asynchronous Rule Execution:** All rule execution is synchronous in the initial version
13. **Transition Metadata:** Transitions do not store additional data (timestamps, execution order, etc.)
14. **Advanced Expression Functions:** Complex functions (ToUpper(), Math.Max(), etc.) not included in initial expression evaluator
15. **Automatic Release Deployment:** Releases require manual approval (not triggered automatically)

## Design Considerations

### State Representation
- States use a flexible key-value structure (`Dictionary<string, object>`) to support various domain models
- State variables support primitive types only: `string`, `int`, `bool`, `float/double`
- Implement `IEquatable<State>` and override `GetHashCode()` for efficient state comparison
- State IDs can be hash-based or sequential (e.g., "S0", "S1", "S2")

### Rule Application Strategy
- Use breadth-first search (BFS) for state exploration (recommended for finding shortest paths)
- All available rules are evaluated for each state (no priority mechanism in initial version)
- Rules match any state where their condition evaluates to true

### Configuration Options
The `BuilderConfig` class should include:
- `MaxDepth` (int, nullable): Maximum depth of exploration. If null, no depth limit
- `MaxStates` (int, nullable): Maximum number of states. If null, no state count limit
- `ExplorationStrategy` (enum: BFS, DFS)
- `GenerateStateIds` (Func<State, string>): Custom ID generator (optional)
- `LogLevel` (enum: INFO, DEBUG, ERROR): Logging verbosity

**Valid Exploration Modes:**
- **Exhaustive Mode:** Both `MaxDepth` and `MaxStates` are null - generates all reachable states
- **State-Limited Mode:** `MaxStates` is set, `MaxDepth` is null - limits total states regardless of depth
- **Dual-Limited Mode:** Both `MaxDepth` and `MaxStates` are set - stops when either limit is reached

**Invalid Configuration (will throw error):**
- **Depth-Only Mode:** `MaxDepth` is set but `MaxStates` is null - **INVALID** (depth limit alone is not sufficient; must include state limit to prevent unbounded exploration)
- **No Configuration:** Both limits are null without explicit exhaustive mode indication - **INVALID** (ambiguous intent)

### Declarative Rule File Format
**JSON Format Only:** Built-in .NET support via System.Text.Json

Example JSON structure:
```json
{
  "rules": [
    {
      "name": "ApproveOrder",
      "condition": "OrderStatus == 'Pending' && Amount < 1000",
      "transformations": {
        "OrderStatus": "Approved",
        "ApprovedDate": "$now"
      }
    }
  ]
}
```

### Expression Evaluation
**Phased Complexity Approach:**
- **Phase 1 (Initial):** Simple comparison operators (`==`, `!=`, `<`, `>`, `<=`, `>=`) and logical operators (`&&`, `||`, `!`)
- **Phase 2:** Basic arithmetic (`+`, `-`, `*`, `/`) and parenthetical expressions
- **Phase 3 (Future):** Complex functions (`ToUpper()`, `Math.Max()`, string manipulation, etc.)

**Implementation:**
- Use a lightweight expression evaluator library (NCalc, DynamicExpresso, or similar)
- Ensure expressions are sandboxed and cannot execute arbitrary code
- Variable references use exact case-sensitive name matching
- Expressions are validated at execution time, not definition time
- Support both code-based and declarative rules in the same state machine

## Development & Release Process

### Continuous Integration
The project follows **trunk-based development** practices with continuous integration:

1. **Unit Test Requirements:**
   - All unit tests must run and pass before merging to trunk (main branch)
   - This requirement must be enforced via GitHub branch protection rules
   - Pull requests cannot be merged if tests fail

2. **Static Code Analysis:**
   - Static inspection checks must be configured to analyze:
     - Code coverage metrics
     - Internal code quality (linting, complexity, etc.)
   - These checks must execute automatically on every pull request
   - Pull requests cannot be merged if quality checks fail

### Continuous Deployment
The project produces a **standalone library/tool** (not a service):

1. **Release Strategy:**
   - Releases require **manual approval** (not automatic on every merge)
   - By default, every change is treated as a **minor version release** (semantic versioning)
   - Version increments can be explicitly specified when needed (e.g., major or patch)

2. **Versioning:**
   - Follow semantic versioning (MAJOR.MINOR.PATCH)
   - Initial product versions shall be **< 1.0.0** (e.g., 0.1.0, 0.2.0) until stable
   - Pre-release versions tagged as **beta** (e.g., 0.1.0-beta, 0.2.0-beta)
   - Version 1.0.0 will be released when explicitly specified (feature-complete and stable)

3. **Release Artifacts:**
   - NuGet package for library distribution
   - Compiled binaries for direct usage
   - Release notes generated from commit messages

## Technical Considerations

1. **Target Framework:** .NET 6.0 or later (LTS version) for broad compatibility
2. **Dependencies:**
   - Core library: Minimal external dependencies
   - `System.Text.Json` for JSON export and declarative rule file parsing (built-in)
   - Expression evaluator library (e.g., NCalc, DynamicExpresso) for declarative rule conditions and transformations
   - No YAML or XML parsers required (JSON only)
3. **Performance:** Use `HashSet<State>` for O(1) duplicate detection; ensure `State.GetHashCode()` is efficient
4. **Memory Management:** Large state spaces could consume significant memory; limits in `BuilderConfig` are critical
5. **Extensibility Pattern:** Interface-based design (`IRule`, `IStateMachineBuilder`) allows mocking and testing
6. **Export Libraries:** Consider using third-party libraries for GraphML/DOT generation or implement minimal spec compliance
7. **Namespace:** `StateMaker` (matches the assembly/repository name)
8. **Security:** Expression evaluation must be sandboxed to prevent code injection attacks in declarative rules
9. **Backward Compatibility:** Library must support state machines serialized from older versions
10. **CI/CD Pipeline:**
    - Platform: **GitHub Actions** (required)
    - Code coverage tool: Free option without licensing costs (e.g., Coverlet)
    - Static analysis: SonarQube (if free tier available), otherwise free alternatives (Roslyn analyzers)
    - Coverage threshold: **80% minimum**
    - Quality metrics: Default recommendations from chosen toolset
11. **Testing:** Unit tests should cover:
    - State equality and hashing
    - Rule application logic
    - Cycle detection
    - Depth and count limits
    - Export format validity
    - Declarative rule parsing and execution
    - Expression evaluation correctness
    - File loading error handling
    - Logging functionality at all levels
    - Mixed rule scenarios (code-based + declarative)
    - Configuration validation (valid and invalid combinations)
    - Initial state validation (null state handling)
    - Error message clarity for validation failures

## Success Metrics

1. **Correctness:** 100% of reachable states are discovered when configured for exhaustive exploration; limited exploration respects configured bounds accurately
2. **Cycle Prevention:** No infinite loops; equivalent states are correctly identified
3. **Performance:** Can build state machines with 1000+ states in under 10 seconds on standard hardware
4. **Usability (Code-based):** Developers can implement a custom rule and build a state machine in under 30 lines of code
5. **Usability (Declarative):** Non-programmers can define a simple rule (condition + transformation) in under 5 minutes
6. **Export Validity:** Exported files are valid and can be opened in target tools (yEd, Graphviz, JSON parsers)
7. **Test Coverage:** Core library achieves ≥80% code coverage
8. **API Clarity:** Junior developers can understand and use the library with minimal documentation
9. **Declarative Rule Parity:** Declarative rules produce identical state machines as equivalent code-based rules
10. **File Loading Reliability:** Rule definition files with valid syntax load successfully 100% of the time with clear error messages for invalid files
11. **CI/CD Reliability:** 100% of commits that pass local tests also pass CI pipeline; no flaky tests
12. **Build Success Rate:** >95% of builds succeed without manual intervention
13. **Logging Functionality:** All three log levels (INFO, DEBUG, ERROR) work correctly with default console output

## Open Questions

All initial questions have been answered and incorporated into the PRD. New questions will be added as they arise during implementation.