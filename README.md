# StateMaker

A C#/.NET library that automatically generates finite state machines from an initial state and transformation rules. StateMaker systematically explores the state space, discovering all reachable states and transitions, making it ideal for test coverage generation, workflow modeling, and system analysis.

## What is StateMaker?

StateMaker automates the tedious and error-prone process of manually defining all possible states in a complex system. Instead of enumerating states by hand, you:

1. Define an **initial state** (a set of variables with values)
2. Define **transformation rules** (how states change)
3. Configure **exploration limits** (exhaustive or bounded)
4. Let StateMaker **generate all reachable states** automatically

## Getting Started with the Command Line

The fastest way to use StateMaker is through the command-line tool. Define your states and rules in a JSON file, then run:

```bash
statemaker build definition.json
statemaker build definition.json --format dot --output graph.dot
statemaker export machine.json --format graphml -o machine.graphml
```

For complete command-line usage, file format reference, and input validation rules, see the **[Console Usage Guide](/docs/statemaker-console-usage.md)**.

## Key Features

- **Automatic State Discovery**: Generate all reachable states from transformation rules
- **Cycle Prevention**: Automatically detect and prevent infinite loops
- **Flexible Rule Definition**:
  - Write custom rules in C# (implement `IRule` interface)
  - Define declarative rules in JSON (no coding required)
  - Mix both approaches in a single state machine
- **Configurable Exploration**:
  - Exhaustive mode (all reachable states)
  - Depth-limited mode
  - State-count-limited mode
- **Export Capabilities**: Export to GraphML, DOT, or JSON for visualization
- **Extensible**: Package and share custom rules as NuGet packages
- **Type-Safe**: Leverages C#/.NET type system for compile-time safety

## Quick Example

### Custom Rule (C# Code)
```csharp
using StateMaker;

public class ApproveOrderRule : IRule
{
    public bool IsAvailable(State state)
    {
        return (string?)state.Variables["OrderStatus"] == "Pending";
    }

    public State Execute(State state)
    {
        var newState = state.Clone();
        newState.Variables["OrderStatus"] = "Approved";
        return newState;
    }
}

// Build the state machine
var initialState = new State();
initialState.Variables["OrderStatus"] = "Pending";

var builder = new StateMachineBuilder();
var rules = new IRule[] { new ApproveOrderRule() };
var config = new BuilderConfig { MaxStates = 100 };
var stateMachine = builder.Build(initialState, rules, config);
```

### Declarative Rule (JSON)
```json
{
  "rules": [
    {
      "name": "ApproveOrder",
      "condition": "OrderStatus == 'Pending'",
      "transformations": {
        "OrderStatus": "Approved"
      }
    }
  ]
}
```

## Use Cases

- **Test Coverage Generation**: Automatically discover all possible application states for comprehensive testing
- **Workflow Modeling**: Model business processes and state transitions
- **System Analysis**: Understand complex system behavior through state exploration
- **Documentation**: Generate visual state diagrams from code
- **Validation**: Verify state machines are complete and correct

## Project Resources

### Documentation
- **[Console Usage Guide](/docs/statemaker-console-usage.md)** - Command-line usage, file formats, and input validation rules
- **[Developer Documentation](/docs/README.md)** - Architecture guides and design documents
- **[Declarative Rules Architecture](/docs/architecture/declarative-rules.md)** - How declarative rules work
- **[Product Requirements Document](/tasks/prd-state-machine-builder.md)** - Complete PRD with requirements, user stories, and technical specifications
- **[Product Description](/product-description.txt)** - High-level product vision

### Workflows
- **[PRD Generation Workflow](/create-prd.md)** - Process for creating Product Requirements Documents
- **[Task Generation Workflow](/generate-tasks.md)** - Process for generating implementation task lists

## Getting Started

> **Note**: StateMaker is currently in development (pre-release version < 1.0.0)

### Prerequisites
- .NET 6.0 or later

### Installation
```bash
# NuGet package (when released)
dotnet add package StateMaker
```

### Basic Usage

1. **Define your initial state**
2. **Create rules** (custom or declarative)
3. **Configure the builder**
4. **Generate the state machine**
5. **Export or analyze** the results

See the [Developer Documentation](/docs/README.md) for detailed examples and guides.

## Architecture Highlights

- **Interface-Based Design**: `IRule` interface allows custom and declarative rules to work identically
- **Polymorphic Rule Execution**: Builder works with `IRule`, agnostic to implementation
- **Immutable States**: Rules return new states, preventing side effects
- **BFS/DFS Exploration**: Configurable exploration strategies
- **Expression Evaluation**: Declarative rules use sandboxed expression evaluation (NCalc)

## Development

### Project Structure
```
/docs                    # Developer documentation
  /architecture          # Architecture documents
  /test                  # Test plans and test tools documentation
/tasks                   # PRDs and task lists
/samples                 # Sample build definition files
/src
  /StateMaker            # Core library
  /StateMaker.Console    # Command-line application
  /StateMaker.Tests      # Unit and end-to-end tests
```

### CI/CD

This project uses **trunk-based development** with continuous integration:

- **Branch**: `main`
- **CI Platform**: GitHub Actions
- **Requirements**:
  - All unit tests must pass
  - Code coverage ≥80%
  - Static analysis checks must pass
- **Releases**: Manual approval, semantic versioning

### Contributing

1. Refer to the [PRD](/tasks/prd-state-machine-builder.md) for requirements and specifications
2. Review [architecture documentation](/docs/architecture/) before implementing features
3. Update documentation when modifying core functionality
4. Ensure tests pass and coverage meets threshold (80%)

## Roadmap

### Phase 1 (Current)
- Core state machine builder
- Custom rule support (`IRule` interface)
- Declarative rule support (JSON)
- Basic expression evaluation
- Export to GraphML, DOT, JSON

### Phase 2 (Future)
- Advanced expression functions
- Additional export formats
- Rule priority mechanism
- Performance optimizations
- Comprehensive examples and guides

### Version 1.0.0
Will be released when the library is feature-complete, stable, and production-ready.

## License

*License information to be added*

## Contact

For questions, issues, or suggestions:
- **GitHub Issues**: [Create an issue](https://github.com/WayneMRoseberry/statemaker/issues)
- **Repository**: [https://github.com/WayneMRoseberry/statemaker](https://github.com/WayneMRoseberry/statemaker)

---

**Status**: Pre-release (v0.x.x-beta) - Active Development
