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
5. **Export Capabilities:** Enable export of generated state machines to standard formats (GraphML, DOT, JSON) for visualization and analysis
6. **Type Safety:** Leverage C#/.NET type system for compile-time safety and IDE support

## User Stories

### Story 1: Test Engineer
As a test engineer, I want to automatically generate all possible states of my application from a set of user actions, so that I can create comprehensive test coverage without manually enumerating every scenario.

### Story 2: Library User
As a developer using the library, I want to define custom rules by implementing a simple interface (isAvailable, execute), so that I can model my domain-specific state transitions without learning complex APIs.

### Story 3: System Analyst
As a system analyst, I want to export the generated state machine to GraphML or DOT format, so that I can visualize and analyze the system behavior using standard tools like yEd or Graphviz.

### Story 4: Developer
As a developer, I want to configure depth and state count limits, so that I can control the exploration process and prevent my application from running out of memory on large or potentially infinite state spaces.

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

## Technical Considerations

1. **Target Framework:** .NET 6.0 or later (LTS version) for broad compatibility
2. **Dependencies:** Minimal external dependencies; consider using `System.Text.Json` for JSON export
3. **Performance:** Use `HashSet<State>` for O(1) duplicate detection; ensure `State.GetHashCode()` is efficient
4. **Memory Management:** Large state spaces could consume significant memory; limits in `BuilderConfig` are critical
5. **Extensibility Pattern:** Interface-based design (`IRule`, `IStateMachineBuilder`) allows mocking and testing
6. **Export Libraries:** Consider using third-party libraries for GraphML/DOT generation or implement minimal spec compliance
7. **Namespace:** `StateMaker` (matches the assembly/repository name)
8. **Testing:** Unit tests should cover:
   - State equality and hashing
   - Rule application logic
   - Cycle detection
   - Depth and count limits
   - Export format validity

## Success Metrics

1. **Correctness:** 100% of reachable states are discovered (up to configured limits)
2. **Cycle Prevention:** No infinite loops; equivalent states are correctly identified
3. **Performance:** Can build state machines with 1000+ states in under 10 seconds on standard hardware
4. **Usability:** Developers can implement a custom rule and build a state machine in under 30 lines of code
5. **Export Validity:** Exported files are valid and can be opened in target tools (yEd, Graphviz, JSON parsers)
6. **Test Coverage:** Core library achieves >90% code coverage
7. **API Clarity:** Junior developers can understand and use the library with minimal documentation

## Open Questions

1. **State Variable Types:** Should `State` support only primitives, or also complex objects? How to handle equality for complex types?
2. **Rule Priority:** If multiple rules are available for a state, should there be a priority mechanism, or are all applied?
3. **Transition Metadata:** Should transitions store additional data (e.g., timestamps, execution order)?
4. **Async Rules:** Should the `IRule.Execute()` method support asynchronous operations?
5. **Partial State Matching:** Should rules support wildcard matching (e.g., "applies to any state where X > 5")?
6. **Logging/Diagnostics:** What level of logging should be built in for debugging state generation?
7. **Version Compatibility:** How should the library handle serialized state machines from older versions?
