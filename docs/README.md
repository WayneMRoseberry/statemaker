# StateMaker Documentation

## Overview

This directory contains developer documentation for the StateMaker library - a C#/.NET state machine builder that generates states and transitions from transformation rules.

## Documentation Structure

### Architecture Documentation (`/architecture`)

Technical architecture documents explaining how the system is designed and how components work together.

**Available Documents:**
- [**Declarative Rules Architecture**](./architecture/declarative-rules.md) - Explains how declarative rule building works, including the use of polymorphism, expression evaluation, and the integration between custom and declarative rules.

**Planned Documents:**
- Builder Architecture - How the state machine builder explores state space
- Expression Evaluation - Details on expression evaluator integration
- State Immutability - Design patterns for immutable state handling
- Export Formats - GraphML, DOT, and JSON export implementations

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
