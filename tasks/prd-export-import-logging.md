# PRD: Tasks 7.0 and 8.0 — Export/Import and Logging

## Overview

Task 7.0: Implement export and import capabilities for StateMachine in JSON, DOT (Graphviz), and GraphML (yEd) formats.

Task 8.0: Implement a logging and diagnostics system for the StateMachineBuilder.

## Task 7.0 — Export and Import

### Interfaces
- `IStateMachineExporter` with `string Export(StateMachine stateMachine)`
- `IStateMachineImporter` with `StateMachine Import(string content)`

### JsonExporter (7.3)
JSON schema:
```json
{
  "startingStateId": "S0",
  "states": {
    "S0": { "Status": "Pending", "Count": 0 },
    "S1": { "Status": "Approved", "Count": 1 }
  },
  "transitions": [
    { "sourceStateId": "S0", "targetStateId": "S1", "ruleName": "Approve" }
  ]
}
```

### JsonImporter (7.4)
- Deserialize JSON back to StateMachine preserving all IDs, variables, and transitions
- Validate required fields, handle type conversion for primitives

### DotExporter (7.6)
- Box-shaped nodes showing state ID and variables
- Directed edges labeled with rule names
- Invisible "start" node with arrow to starting state
- Valid DOT syntax parseable by Graphviz

### GraphMlExporter (7.8)
- yEd-compatible GraphML XML
- ShapeNode elements with state labels
- Edge labels with rule names
- Visual properties (colors, shapes) for yEd rendering

### Design Decisions
- Uses `System.Text.Json` for JSON (already a dependency)
- DOT and GraphML are export-only (no import)
- Variable values serialized as their natural JSON types (string, int, bool, double)

## Task 8.0 — Logging and Diagnostics

### ILogger Interface (8.1)
```csharp
public interface IStateMachineLogger
{
    void Info(string message);
    void Debug(string message);
    void Error(string message);
}
```
Note: Named `IStateMachineLogger` to avoid conflict with `Microsoft.Extensions.Logging.ILogger`.

### ConsoleLogger (8.2)
- Writes to Console.Out
- Respects configured LogLevel
- INFO: logs Info and Error
- DEBUG: logs all (Info, Debug, Error)
- ERROR: logs Error only

### Builder Integration (8.3-8.4)
- `StateMachineBuilder` accepts optional `IStateMachineLogger` parameter
- Logs: state discovery, rule application, cycle detection, limit reached
- Default: no logging (null logger / no-op)
- LogLevel already exists on BuilderConfig

### Extensibility (8.5)
- Users provide custom `IStateMachineLogger` implementations
- Builder accepts logger via constructor or Build method parameter
