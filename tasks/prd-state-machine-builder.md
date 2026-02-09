# Product Requirements Document: State Machine Builder

## Introduction/Overview

The State Machine Builder is a C#/.NET library that generates all possible states and transitions of a finite state machine from an initial state and a set of transformation rules. The tool explores the state space systematically, creating a complete state machine definition that can be used for various purposes including software testing, workflow modeling, and system analysis.

**Problem it solves:** Manually defining all possible states and transitions in a complex system is time-consuming, error-prone, and difficult to maintain. This tool automates the discovery of states by applying rules iteratively, ensuring completeness and correctness.

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
As a test engineer, I want to automatically generate all possible states of my application from a set of user actions, so that I can create comprehensive test coverage without manually enumerating every scenario.

### Story 2: Library User
As a developer using the library, I want to define custom rules by implementing a simple interface (isAvailable, execute), so that I can model my domain-specific state transitions without learning complex APIs.

### Story 3: System Analyst
As a system analyst, I want to export the generated state machine to GraphML or DOT format, so that I can visualize and analyze the system behavior using standard tools like yEd or Graphviz.

### Story 4: Developer
As a developer, I want to configure depth and state count limits, so that I can control the exploration process and prevent my application from running out of memory on large or potentially infinite state spaces.

### Story 5: Business Analyst
As a business analyst without programming expertise, I want to define state machine rules using a declarative format (via API or file), so that I can model workflows and processes without writing C# code.

## User Stories in Gherkin Format

### Feature: Automated State Machine Generation

**Scenario 1: Test Engineer generates comprehensive test coverage**
```gherkin
Feature: Generate all possible application states for testing

Scenario: Generate test states from user actions
  Given I am a test engineer with a set of user action rules
  And I have defined an initial application state
  When I build the state machine with these rules
  Then all possible reachable states are generated
  And I can create comprehensive test coverage without manual enumeration
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
7. The builder must implement a `Build(State initialState, Rule[] rules, BuilderConfig config)` method
8. The builder must start from the initial state and iteratively apply all available rules
9. For each new state generated, the builder must check if an equivalent state already exists
10. If an equivalent state exists, the builder must create a transition to the existing state and **stop exploring that path** (cycle prevention)
11. If the state is new, the builder must add it to the state machine and continue exploration
12. The builder must respect the depth limit specified in `BuilderConfig` (maximum levels of transitions from initial state)
13. The builder must respect the state count limit specified in `BuilderConfig` (maximum total states)
14. The builder must stop exploration when either limit is reached
15. The builder must assign unique IDs to each state

### Export Capabilities

16. The system must provide an export mechanism to serialize the state machine to JSON format
17. The system must provide an export mechanism to generate GraphML format for tools like yEd
18. The system must provide an export mechanism to generate DOT format for Graphviz
19. Each export format must include all states, transitions, and rule names

### Namespace and Extensibility

20. All interfaces (`IRule`, `IStateMachineBuilder`) and core classes (`State`, `StateMachine`, `BuilderConfig`, `Transition`) must be in the `StateMaker` namespace
21. The namespace must be designed to allow external assemblies to reference it and implement custom `IRule` implementations
22. Rule names should be automatically derived from the rule class name (or configurable)

### Declarative Rule Definition

23. The system must provide a declarative rule definition mechanism that does not require writing custom C# classes
24. A declarative rule definition must include:
    - Rule name (string identifier)
    - Availability condition (boolean expression evaluated against state variables)
    - Variable transformations (mapping of variable names to new values or expressions)
25. The system must provide an API method to create declarative rules programmatically (e.g., `DefineRule(name, condition, transformations)`)
26. The system must support boolean expressions for conditions using standard operators:
    - Equality: `==`, `!=`
    - Comparison: `<`, `>`, `<=`, `>=`
    - Logical: `&&`, `||`, `!`
    - Example: `"Age >= 18 && Status == 'Active'"`
27. The system must support transformation expressions that can:
    - Set variables to literal values: `Status = "Approved"`
    - Reference current state variables: `Count = Count + 1`
    - Use basic arithmetic: `+`, `-`, `*`, `/`
28. The system must provide a file loader that reads rule definitions from an external file
29. The file format must be structured and human-readable (JSON, YAML, or XML)
30. The file loader must validate rule definitions and provide clear error messages for invalid syntax
31. Declarative rules must implement the same `IRule` interface as code-based rules, ensuring they work identically in the builder

## Non-Goals (Out of Scope)

The following are explicitly **not** included in the initial version:

1. **Built-in Visualization UI:** No graphical rendering within the library itself (export formats allow external visualization)
2. **Rule Conflict Resolution:** No automatic handling of conflicting rules; developers must ensure rules are deterministic
3. **Parallel Rule Application:** Rules are applied sequentially; no concurrent state generation
4. **State Machine Execution Engine:** The library builds state machines but does not execute or simulate them
5. **Undo/Redo of State Transitions:** The builder is a forward-only exploration
6. **Incremental Building:** Each `Build()` call starts fresh; no support for adding rules to an existing state machine
7. **Multi-language Support:** Initial version is C#/.NET only (may expand in future versions)
8. **Advanced Export Formats:** Formats like SCXML, Petri nets are not included initially

## Design Considerations

### State Representation
- States should use a flexible key-value structure (e.g., `Dictionary<string, object>`) to support various domain models
- Consider implementing `IEquatable<State>` and overriding `GetHashCode()` for efficient state comparison and deduplication
- State IDs could be hash-based or sequential (e.g., "S0", "S1", "S2")

### Rule Application Strategy
- Use breadth-first search (BFS) or depth-first search (DFS) for state exploration (BFS recommended for finding shortest paths)
- Allow configuration to choose exploration strategy in `BuilderConfig`

### Configuration Options
The `BuilderConfig` class should include:
- `MaxDepth` (int, nullable): Maximum depth of exploration
- `MaxStates` (int, nullable): Maximum number of states
- `ExplorationStrategy` (enum: BFS, DFS)
- `GenerateStateIds` (Func<State, string>): Custom ID generator (optional)

### Declarative Rule File Format
For declarative rule definitions loaded from files, consider:
- **JSON Format:** Most common, built-in .NET support via System.Text.Json
- **YAML Format:** More human-readable, requires third-party library (YamlDotNet)
- **XML Format:** Verbose but schema-validatable

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
- Use a lightweight expression evaluator for boolean conditions and transformations
- Consider libraries like NCalc, DynamicExpresso, or CSharpCodeProvider for expression parsing
- Ensure expressions are sandboxed and cannot execute arbitrary code
- Support variable references using simple syntax (e.g., variable names without quotes)

## Technical Considerations

1. **Target Framework:** .NET 6.0 or later (LTS version) for broad compatibility
2. **Dependencies:**
   - Core library: Minimal external dependencies
   - `System.Text.Json` for JSON export and declarative rule file parsing
   - Expression evaluator library (e.g., NCalc, DynamicExpresso) for declarative rule conditions and transformations
   - Optional: YAML parser (YamlDotNet) if YAML format is supported
3. **Performance:** Use `HashSet<State>` for O(1) duplicate detection; ensure `State.GetHashCode()` is efficient
4. **Memory Management:** Large state spaces could consume significant memory; limits in `BuilderConfig` are critical
5. **Extensibility Pattern:** Interface-based design (`IRule`, `IStateMachineBuilder`) allows mocking and testing
6. **Export Libraries:** Consider using third-party libraries for GraphML/DOT generation or implement minimal spec compliance
7. **Namespace:** `StateMaker` (matches the assembly/repository name)
8. **Security:** Expression evaluation must be sandboxed to prevent code injection attacks in declarative rules
9. **Testing:** Unit tests should cover:
   - State equality and hashing
   - Rule application logic
   - Cycle detection
   - Depth and count limits
   - Export format validity
   - Declarative rule parsing and execution
   - Expression evaluation correctness
   - File loading error handling

## Success Metrics

1. **Correctness:** 100% of reachable states are discovered (up to configured limits)
2. **Cycle Prevention:** No infinite loops; equivalent states are correctly identified
3. **Performance:** Can build state machines with 1000+ states in under 10 seconds on standard hardware
4. **Usability (Code-based):** Developers can implement a custom rule and build a state machine in under 30 lines of code
5. **Usability (Declarative):** Non-programmers can define a simple rule (condition + transformation) in under 5 minutes
6. **Export Validity:** Exported files are valid and can be opened in target tools (yEd, Graphviz, JSON parsers)
7. **Test Coverage:** Core library achieves >90% code coverage
8. **API Clarity:** Junior developers can understand and use the library with minimal documentation
9. **Declarative Rule Parity:** Declarative rules produce identical state machines as equivalent code-based rules
10. **File Loading Reliability:** Rule definition files with valid syntax load successfully 100% of the time with clear error messages for invalid files

## Open Questions

1. **State Variable Types:** Should `State` support only primitives, or also complex objects? How to handle equality for complex types?
2. **Rule Priority:** If multiple rules are available for a state, should there be a priority mechanism, or are all applied?
3. **Transition Metadata:** Should transitions store additional data (e.g., timestamps, execution order)?
4. **Async Rules:** Should the `IRule.Execute()` method support asynchronous operations?
5. **Partial State Matching:** Should rules support wildcard matching (e.g., "applies to any state where X > 5")?
6. **Logging/Diagnostics:** What level of logging should be built in for debugging state generation?
7. **Version Compatibility:** How should the library handle serialized state machines from older versions?
8. **Declarative File Format:** Should the initial version support JSON only, or also YAML/XML? What's the priority?
9. **Expression Language Complexity:** How complex should expressions be? Should they support functions (e.g., `ToUpper()`, `Math.Max()`)?
10. **Declarative Rule Validation:** Should the library validate expressions at definition time or only at execution time?
11. **Mixed Rules:** Can a state machine be built with both code-based and declarative rules simultaneously?
12. **Expression Variable Scoping:** How should declarative expressions reference state variables? Case-sensitive? String interpolation?
