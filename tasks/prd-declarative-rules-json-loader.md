# PRD: Task 6.0 — Declarative Rules and JSON File Loader

## Overview

Implement a `DeclarativeRule` class that implements `IRule` using expression strings evaluated by `IExpressionEvaluator`, and a `RuleFileLoader` that reads JSON files to produce initial state and rule arrays. Also provide a programmatic `RuleBuilder` API for creating declarative rules without files.

## Dependencies

- Task 5.0 (Expression Evaluation System) — completed
- `IExpressionEvaluator` / `ExpressionEvaluator` using NCalcSync v5.11.0
- `IRule` interface with `IsAvailable`, `Execute`, `GetName`
- `State` class with `Dictionary<string, object?>` Variables and `Clone()`

## Sub-tasks

### 6.1 — DeclarativeRule class
- Implements `IRule`
- Constructor: `(string name, string condition, Dictionary<string, string> transformations, IExpressionEvaluator evaluator)`
- Stores name, condition expression string, transformations (variable name → expression string)
- Overrides `GetName()` to return the configured name

### 6.2 — DeclarativeRule.IsAvailable()
- Casts `State.Variables` to `Dictionary<string, object>` (stripping nullable)
- Calls `evaluator.EvaluateBoolean(condition, variables)`
- Returns result

### 6.3 — DeclarativeRule.Execute()
- Clones the input state
- Evaluates each transformation expression against the **original** state's variables (not the clone's)
- Sets the evaluated results on the clone's variables
- Returns the clone

### 6.4 — DeclarativeRule unit tests
- Condition true/false
- Single and multiple transformations
- Transformations evaluated against original state (not intermediate)
- Input state immutability
- GetName() returns configured name
- Error cases: invalid condition, invalid transformation expression

### 6.5–6.7 — RuleFileLoader (JSON parsing)
- `RuleFileLoader` class with method: `(State? initialState, IRule[] rules) LoadFromFile(string filePath)`
- Also: `(State? initialState, IRule[] rules) LoadFromJson(string json)` for testability
- JSON schema:
  ```json
  {
    "initialState": { "Status": "Pending", "Count": 0 },
    "rules": [
      {
        "name": "ApproveRule",
        "condition": "[Status] == 'Pending'",
        "transformations": { "Status": "'Approved'" }
      },
      {
        "type": "custom",
        "assemblyPath": "path/to/assembly.dll",
        "className": "MyNamespace.MyRule"
      }
    ]
  }
  ```
- `initialState` is optional — returns null if absent
- Rules without `type` or with `type: "declarative"` → `DeclarativeRule`
- Uses `System.Text.Json` (built-in, no extra dependency)

### 6.8 — Custom rule loading via reflection
- `type: "custom"` rules specify `assemblyPath` and `className`
- Load assembly via `Assembly.LoadFrom()`
- Find type, verify it implements `IRule`
- Instantiate via parameterless constructor using `Activator.CreateInstance()`

### 6.9 — Validation and error messages
- Missing `name` for declarative rules
- Missing `condition` for declarative rules
- Invalid JSON syntax
- `className` not found in assembly
- Class doesn't implement `IRule`
- No parameterless constructor
- File not found

### 6.10 — RuleFileLoader tests
- Valid declarative rules
- Valid custom rules (using a test assembly rule)
- Mixed declarative + custom rules
- Missing initialState (returns null)
- Present initialState with various types
- All error cases from 6.9

### 6.11–6.12 — Programmatic RuleBuilder API
- Static `RuleBuilder` class with fluent or simple factory method
- `RuleBuilder.DefineRule(string name, string condition, Dictionary<string, string> transformations)` → `DeclarativeRule`
- Unit tests for the programmatic API

## Design Decisions

- Use `System.Text.Json` (no extra NuGet dependency needed for .NET 6.0)
- `RuleFileLoader` takes `IExpressionEvaluator` in constructor (dependency injection)
- Transformations evaluated against **original** state variables to avoid order-dependent behavior
- `DeclarativeRule` is a public, non-sealed class in the `StateMaker` namespace
