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
As a developer using the library, I want to define custom rules by implementing the IRule interface (IsAvailable, Execute), so that I can model my domain-specific state transitions and ensure my Execute method returns new immutable states.

### Story 3: System Analyst
As a system analyst, I want to export the generated state machine to GraphML or DOT format, so that I can visualize and analyze the system behavior using tools like yEd or Graphviz.

### Story 4: Developer
As a developer, I want to configure depth and state count limits, so that I can control the exploration process and prevent my application from running out of memory on large or potentially infinite state spaces.

### Story 5: Business Analyst
As a business analyst without programming expertise, I want to define state machine rules using a declarative format via file so that I can model workflows and processes without writing C# code.

### Story 6: Developer - Configuration Validation
As a developer using the library, I want the builder to validate my configuration settings and initial state before attempting to build the state machine, so that I receive clear error messages about invalid configurations rather than unexpected runtime behavior or infinite loops.

### Story 7: Developer - Sharing Custom Rules
As a developer who has implemented domain-specific custom rules, I want to package my rules as a separate assembly or NuGet package, so that other developers can reuse my rules in their own state machine projects and combine them with their own custom or declarative rules.

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

Scenario: Developer creates an initial state programmatically
  Given I am a developer who needs to define an initial application state
  And I have imported the StateMaker namespace
  When I create a new State object
  And I add variable "OrderStatus" with value "Pending"
  And I add variable "Amount" with value 500
  And I add variable "IsApproved" with value false
  Then the state contains three variables with the specified values
  And the state can be passed to builder.Build as the initial state

Scenario: Implement a custom rule class
  Given I am a developer who needs to model domain-specific transitions
  And I have imported the StateMaker namespace
  When I create a class that implements the IRule interface
  And I implement the IsAvailable(State state) method to check if the rule applies
  And I implement the Execute(State state) method to return the new state
  Then I have a custom rule class ready to use

Scenario: Developer implements custom rule AddOption.IsAvailable so that the rule will fire when "OptionList <> Empty"
  Given I am implementing the AddOption rule class
  And the rule should only apply when the OptionList is not empty
  When I implement the IsAvailable(State state) method
  And I check if state contains a variable "OptionList"
  And I verify that "OptionList" is not empty
  Then the method returns true when OptionList has items
  And the method returns false when OptionList is empty or missing

Scenario: Developer implements AddOption.Execute so that it returns a new state when triggered
  Given I am implementing the AddOption rule class
  And the rule should add an option to the current state
  When I implement the Execute(State state) method
  And I create a new State object based on the input state
  And I modify the appropriate variables to reflect adding an option
  Then the method returns the new state with the option added
  And the original state remains unchanged

Scenario: Use custom rules with the builder
  Given I have implemented one or more custom rule classes
  And I have created an initial state
  And I have created a BuilderConfig with appropriate limits
  When I create an array of my custom rule instances
  And I call builder.Build(initialState, rules, config)
  Then the builder uses my custom rules to generate the state machine
  And my custom rule logic determines the state transitions

Scenario: Developer calls custom rule from declarative state machine definition
  Given I have implemented a custom rule class (e.g., ComplexValidationRule)
  And I have a declarative rule definition file with multiple declarative rules
  When I load the declarative rules from the file
  And I create an instance of my custom rule
  And I combine both declarative and custom rules into a single rule array
  And I call builder.Build(initialState, combinedRules, config)
  Then the builder uses both declarative and custom rules together
  And the state machine includes transitions from both rule types
  And the custom rule executes alongside the declarative rules

Scenario: Share custom rules as a reusable library
  Given I have implemented custom rule classes for my domain
  When I compile my rules into a separate assembly/NuGet package
  And I reference the StateMaker namespace in my package
  Then other developers can reference my package
  And they can use my custom rules by creating instances and passing them to the builder
  And they can combine my rules with their own custom or declarative rules
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

Scenario: Import state machine from JSON and re-export to different format
  Given I have a previously generated state machine exported to JSON format
  When I import the state machine from the JSON file
  And I export it to GraphML format
  Then I receive a valid GraphML file
  And the state machine structure is preserved from the original
  And I can visualize it in yEd without loss of information
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

Scenario: Business analyst authors rules and initial state using declarative JSON file
  Given I am a business analyst without programming skills
  And I want to define an initial state and a rule named "ApproveOrder"
  When I create a JSON file with an initialState object
  And I define the initial state with variable "OrderStatus" = "Pending" and "Amount" = 500
  And I add a rules array with a rule entry named "ApproveOrder"
  And I set the condition to "OrderStatus == 'Pending'"
  And I set the transformation to "OrderStatus" = "Approved"
  And I provide the JSON file to a developer
  Then the developer loads the file using the file loader
  And the loader returns both the initial state and an array of IRule instances
  And the developer can build a state machine using the initial state and rules from my file
  And I did not need to write any C# code

Scenario: Developer loads business analyst's definition file and builds state machine
  Given a business analyst has authored a JSON definition file
  And the file contains an initial state and multiple rule definitions
  When a developer passes the file path to the file loader
  Then the loader returns a State object matching the initial state defined in the JSON
  And the loader returns an IRule array with one entry per rule defined in the file
  And each returned rule has the name specified in the JSON
  And the developer passes the returned state, rules, and a config to builder.Build
  And the builder produces a state machine using the loaded initial state and rules
```

## Functional Requirements

### Core Data Structures

1. The system must provide a `State` class that stores a set of variables with their values
2. The `State` class must allow programmatic construction by providing variable names and their values
3. The `State` class must implement equality comparison to determine if two states are equivalent (same variables with same values)
4. The system must provide a `Rule` interface with two methods:
   - `bool IsAvailable(State state)` - returns true if the rule can be applied to the given state
   - `State Execute(State state)` - returns a new state resulting from applying the rule
5. The system must provide a `StateMachine` class with the following properties:
   - `Dictionary<string, State> States` - all discovered states, keyed by unique ID
   - `string StartingStateId` - the ID of the initial state
   - `List<Transition> Transitions` - list of state transitions with source ID, target ID, and rule name
6. The system must provide a `BuilderConfig` class for configuration settings

### State Machine Builder

7. The system must provide a `StateMachineBuilder` class implementing an `IStateMachineBuilder` interface
8. The builder must implement a `Build(State initialState, Rule[] rules, BuilderConfig config)` method that requires a configuration parameter
9. The builder must start from the initial state and iteratively apply all available rules
10. For each new state generated, the builder must check if an equivalent state already exists
11. If an equivalent state exists, the builder must create a transition to the existing state and **stop exploring that path** (cycle prevention)
12. If the state is new, the builder must add it to the state machine and continue exploration
13. The builder must respect the depth limit specified in `BuilderConfig.MaxDepth` if set (maximum levels of transitions from initial state)
14. The builder must respect the state count limit specified in `BuilderConfig.MaxStates` if set (maximum total states)
15. The builder must stop exploration when either limit is reached, if configured
16. If both `MaxDepth` and `MaxStates` are null (not set), the builder performs **exhaustive exploration** generating all reachable states
17. The builder must assign unique IDs to each state

### Configuration Validation

18. The builder must validate that the initial state parameter is not null before beginning exploration
19. If the initial state is null, the builder must throw an `ArgumentNullException` with the message "No initial state provided"
20. The builder must validate the configuration settings before beginning exploration
21. Valid configuration combinations are:
    - **Exhaustive mode:** Both `MaxDepth` and `MaxStates` are null
    - **State-limited mode:** `MaxStates` is set (with or without `MaxDepth`)
    - **Dual-limited mode:** Both `MaxDepth` and `MaxStates` are set
22. Invalid configuration combinations that must result in an error:
    - **Depth-only mode:** `MaxDepth` is set but `MaxStates` is null (invalid - requires state limit)
    - **No limits configured:** Both `MaxDepth` and `MaxStates` are null AND exhaustive mode is not explicitly enabled
23. When an invalid configuration is detected, the builder must throw an `InvalidOperationException` with the message "Invalid configuration: {specific reason}"
24. The error message must clearly indicate which configuration requirement was violated

### Export and Import Capabilities

25. The system must provide an export mechanism to serialize the state machine to JSON format
26. The system must provide an export mechanism to generate GraphML format for tools like yEd
27. The system must provide an export mechanism to generate DOT format for Graphviz
28. Each export format must include all states, transitions, and rule names
29. The system must provide an import mechanism to deserialize a state machine from JSON format
30. The imported state machine must preserve all states, transitions, state IDs, and rule names from the original
31. An imported state machine must be exportable to any supported format (JSON, GraphML, DOT) without loss of information

### Namespace and Extensibility

32. All interfaces (`IRule`, `IStateMachineBuilder`) and core classes (`State`, `StateMachine`, `BuilderConfig`, `Transition`) must be in the `StateMaker` namespace
33. The namespace must be designed to allow external assemblies to reference it and implement custom `IRule` implementations
34. Rule names should be automatically derived from the rule class name (or configurable)

### Declarative Rule Definition

35. The system must provide a declarative rule definition mechanism that does not require writing custom C# classes
36. A declarative rule definition must include:
    - Rule name (string identifier)
    - Availability condition (boolean expression evaluated against state variables)
    - Variable transformations (mapping of variable names to new values or expressions)
37. The system must provide an API method to create declarative rules programmatically (e.g., `DefineRule(name, condition, transformations)`)
38. The system must support boolean expressions for conditions using standard operators (initial version):
    - Equality: `==`, `!=`
    - Comparison: `<`, `>`, `<=`, `>=`
    - Logical: `&&`, `||`, `!`
    - Example: `"Age >= 18 && Status == 'Active'"`
39. The system must support transformation expressions that can (initial version):
    - Set variables to literal values: `Status = "Approved"`
    - Reference current state variables: `Count = Count + 1`
    - Use basic arithmetic: `+`, `-`, `*`, `/`, and parenthetical expressions
40. The system must provide a file loader that reads definition files from an external JSON file
41. The JSON definition file must support an optional `initialState` object that defines the initial state variables and values
42. When an `initialState` is present in the JSON file, the file loader must return a `State` object constructed from its contents
43. When an `initialState` is absent from the JSON file, the developer must provide an initial state programmatically
44. The file format must be JSON only (structured and human-readable)
45. The file loader must validate definitions at execution time and provide clear error messages for invalid syntax
46. Declarative rules must implement the same `IRule` interface as code-based rules, ensuring they work identically in the builder
47. State variable references in expressions must be case-sensitive exact name matches
48. A declarative state machine definition must support mixed rules (both declarative and programmatically-defined rules)

### Custom Rule Implementation

49. Custom rule implementations must not modify the input state in the Execute method
50. The Execute method must return a new State object, leaving the original state unchanged (immutability)
51. Custom rules must be implementable in external assemblies that reference the StateMaker namespace
52. Custom rules packaged in external assemblies must work identically to rules defined in the main application
53. The system must support loading and using custom rules from referenced NuGet packages or DLLs

### Logging and Diagnostics

54. The system must provide a logging mechanism with three severity levels:
    - INFO: General information about state machine building progress
    - DEBUG: Detailed information for in-depth investigation
    - ERROR: Error conditions
55. The default logging level must be INFO and ERROR (DEBUG disabled by default)
56. The logging system must support extensible loggers to allow custom destinations
57. The default logger must output to the console
58. State variables must support only primitive types in the initial version (strings, integers, booleans, floats)

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

### Custom Rule Implementation Best Practices
- **State Immutability:** Execute methods must create new State objects rather than modifying input states
- **Variable Checking:** IsAvailable should check for variable existence before accessing values to avoid exceptions
- **Return Values:** IsAvailable should return false (not throw) when a state doesn't meet rule conditions
- **Null Handling:** Custom rules should handle null or missing state variables gracefully
- **Packaging:** Custom rules can be packaged in separate assemblies/NuGet packages for reuse
- **Namespace Reference:** External rule assemblies must reference the StateMaker namespace
- **Combining Rules:** Custom rules from external packages can be mixed with local custom rules and declarative rules in a single Build call

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

### Declarative Definition File Format
**JSON Format Only:** Built-in .NET support via System.Text.Json

The JSON file can contain both an initial state definition and rule definitions:

Example JSON structure:
```json
{
  "initialState": {
    "OrderStatus": "Pending",
    "Amount": 500,
    "IsApproved": false
  },
  "rules": [
    {
      "name": "ApproveOrder",
      "condition": "OrderStatus == 'Pending' && Amount < 1000",
      "transformations": {
        "OrderStatus": "Approved",
        "IsApproved": true
      }
    }
  ]
}
```

- The `initialState` object is optional. If omitted, the developer must provide an initial state programmatically.
- The `rules` array is required and must contain at least one rule definition.

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
    - Export format validity (JSON, GraphML, DOT)
    - Import from JSON format
    - Round-trip JSON export/import (data preservation)
    - Re-export imported state machines to other formats
    - Declarative rule parsing and execution
    - Expression evaluation correctness
    - File loading error handling
    - Logging functionality at all levels
    - Mixed rule scenarios (code-based + declarative)
    - Configuration validation (valid and invalid combinations)
    - Initial state validation (null state handling)
    - Error message clarity for validation failures
    - Custom rule IsAvailable method returning correct boolean values
    - Custom rule Execute method creating new states (immutability)
    - Custom rule Execute method not modifying input state
    - Custom rules from external assemblies/packages
    - Combining custom rules from multiple sources (local + external + declarative)
12. **Documentation:**
    - Developer documentation is maintained in the `/docs` directory
    - Architecture documents in `/docs/architecture/` explain design decisions and component interactions
    - When implementing features, update relevant architecture documents to reflect actual implementation
    - When modifying core functionality (IRule, State, Builder, etc.), review and update corresponding documentation
    - Add new architecture documents for significant features or design patterns
    - Keep documentation synchronized with code changes during PRs
    - Current architecture documents:
      - `/docs/architecture/declarative-rules.md` - Declarative rule building architecture
    - See `/docs/README.md` for documentation standards and contribution guidelines

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
14. **State Immutability:** Custom rule Execute methods never modify input states; all tests verify new state objects are returned
15. **Custom Rule Reusability:** Custom rules packaged as external assemblies can be referenced and used without modification
16. **Rule Composition:** Developers can successfully combine custom rules from multiple sources (local, external packages, declarative) in a single state machine build

## Open Questions

All initial questions have been answered and incorporated into the PRD. New questions will be added as they arise during implementation.