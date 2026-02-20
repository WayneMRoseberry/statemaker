# StateMaker Documentation

## Overview

This directory contains developer documentation for the StateMaker library - a C#/.NET state machine builder that generates states and transitions from transformation rules.

## Documentation Structure

### Architecture Documentation (`/architecture`)

Technical architecture documents explaining how the system is designed and how components work together.

**Available Documents:**
- [**Declarative Rules Architecture**](./architecture/declarative-rules.md) - How declarative rule building works, including polymorphism, expression evaluation, and integration between custom and declarative rules
- [**Builder Architecture**](./architecture/builder-architecture.md) - How the state machine builder explores state space, including validation, BFS/DFS strategies, cycle detection, and logging
- [**Expression Evaluation**](./architecture/expression-evaluation.md) - Expression evaluator integration, supported operators, variable resolution, and security sandboxing
- [**State Immutability**](./architecture/state-immutability.md) - Why immutability matters, the Clone pattern, common mistakes, testing patterns, and correct equality implementation
- [**Export Formats**](./architecture/export-formats.md) - JSON, DOT, and GraphML export/import specifications, state and transition representation, visualization options, and format comparison

### Console Usage
- [**Console Usage Guide**](./statemaker-console-usage.md) - Command-line usage, build definition file format, output formats, and input validation rules

### Test Documentation (`/test`)

Documentation for test plans and test tooling.

**Available Documents:**
- [**StateMachineBuilder Test Plan**](./test/StateMachineBuilder_test_plan.md) - Test plan covering shapes, rule combinations, and oracle strategies
- [**Test Tools Guide**](./test/test-tools-guide.md) - How to use TestCaseGenerator, TestBatteryExecutor, and ReverseRuleGenerator for automated state machine testing

### API Documentation (`/api`)
*Coming soon* - Auto-generated API documentation from XML comments

### User Guides (`/guides`)
*Coming soon* - Step-by-step guides for common tasks

### Examples (`/examples`)
*Coming soon* - Code examples demonstrating library usage

## Quick Links

- [Product Requirements Document](../tasks/prd-state-machine-builder.md)
- [Project Description](../product-description.txt)
- [Main Repository](https://github.com/WayneMRoseberry/statemaker)

## Contributing to Documentation

When adding new documentation:

1. **Architecture Documents**: Place in `/docs/architecture/`
   - Use descriptive filenames (e.g., `state-comparison.md`)
   - Include code examples where applicable
   - Reference related PRD sections

2. **API Documentation**: Auto-generated from XML comments
   - Add XML doc comments to public APIs
   - Follow standard C# documentation format

3. **User Guides**: Place in `/docs/guides/`
   - Focus on "how-to" scenarios
   - Include complete working examples
   - Target junior developers as the audience

4. **Examples**: Place in `/docs/examples/`
   - Provide complete, runnable code
   - Include comments explaining key concepts
   - Show both simple and advanced usage

## Documentation Standards

- Use Markdown format (`.md`)
- Include code examples with syntax highlighting
- Add diagrams for complex concepts (PlantUML, Mermaid)
- Link to related documents
- Keep PRD references updated
- Target audience: Junior to mid-level developers

## Questions or Feedback

For questions about the documentation or suggestions for new topics, please create an issue in the GitHub repository.
