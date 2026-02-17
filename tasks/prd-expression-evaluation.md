# PRD: Expression Evaluation System (Task 5.0)

## Overview

Implement an expression evaluation system that powers declarative rules. The system evaluates boolean condition expressions (for `IsAvailable()`) and value transformation expressions (for `Execute()`) against state variables.

## Library Choice

**NCalcSync v5.11.0** — Synchronous NCalc expression evaluator for .NET.

Rationale:
- Actively maintained (5M+ downloads)
- Sandboxed by default (no file/network/reflection access)
- Supports all required operators (comparison, logical, arithmetic)
- Lightweight, minimal dependencies
- .NET 6.0+ compatible
- Clear error messages for invalid expressions

## Implementation Plan

### 5.1 — IExpressionEvaluator Interface
- `bool EvaluateBoolean(string expression, Dictionary<string, object> variables)`
- `object Evaluate(string expression, Dictionary<string, object> variables)`

### 5.2 — Add NCalcSync dependency
- `dotnet add package NCalcSync`

### 5.3 — Comparison operators: `==`, `!=`, `<`, `>`, `<=`, `>=`
### 5.4 — Logical operators: `&&`, `||`, `!`
### 5.5 — Literal values: strings (`'Pending'`), integers, booleans, floats
### 5.6 — Variable resolution: case-sensitive exact match
### 5.7 — Arithmetic operators: `+`, `-`, `*`, `/` and parentheses
### 5.8 — Error handling: undefined variable, type mismatch, division by zero, invalid syntax
### 5.9 — Sandboxing: expressions cannot access file system, network, reflection, or execute arbitrary code
### 5.10 — Unit tests for all operators, variables, compound expressions, and error cases

## Files Created/Modified

- `src/StateMaker/IExpressionEvaluator.cs` — Interface
- `src/StateMaker/ExpressionEvaluator.cs` — NCalc-based implementation
- `src/StateMaker/StateMaker.csproj` — Add NCalcSync package reference
- `src/StateMaker.Tests/ExpressionEvaluatorTests.cs` — Unit tests
- `tasks/tasks-state-machine-builder.md` — Check off sub-tasks

## NCalc Expression Syntax Notes

NCalc uses slightly different syntax than C#:
- String literals use single quotes: `'Pending'`
- Logical AND: `&&` or `and`
- Logical OR: `||` or `or`
- Logical NOT: `!` or `not`
- Comparison operators: standard `==`, `!=`, `<`, `>`, `<=`, `>=`
- Arithmetic: `+`, `-`, `*`, `/`
- Parentheses: supported

Variable parameters are set via `Expression.Parameters` dictionary.
