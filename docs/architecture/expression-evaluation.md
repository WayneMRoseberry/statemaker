# Expression Evaluation Architecture

## Overview

The expression evaluation system powers declarative rules in StateMaker. It evaluates boolean condition expressions for `IsAvailable()` and value transformation expressions for `Execute()`, all against the current state's variables.

## Role in the System

```
JSON Rule Definition
  ├─ condition: "OrderStatus == 'Pending' && Amount < 1000"
  └─ transformations: { "OrderStatus": "Approved", "Count": "Count + 1" }
        │
        ▼
DeclarativeRule (implements IRule)
  ├─ IsAvailable() → IExpressionEvaluator.EvaluateBoolean(condition, state.Variables)
  └─ Execute()     → IExpressionEvaluator.Evaluate(transformation, state.Variables)
```

## IExpressionEvaluator Interface

```csharp
public interface IExpressionEvaluator
{
    /// Evaluates a boolean expression against state variables.
    /// Returns true or false.
    bool EvaluateBoolean(string expression, Dictionary<string, object> variables);

    /// Evaluates a value expression against state variables.
    /// Returns the computed value (string, int, bool, or float/double).
    object Evaluate(string expression, Dictionary<string, object> variables);
}
```

## Expression Types

### Condition Expressions (Boolean)

Used in `IsAvailable()` to determine if a rule can fire.

| Expression | State Variables | Result |
|---|---|---|
| `OrderStatus == 'Pending'` | `{ OrderStatus: "Pending" }` | `true` |
| `Amount < 1000` | `{ Amount: 500 }` | `true` |
| `OrderStatus == 'Pending' && Amount < 1000` | `{ OrderStatus: "Pending", Amount: 500 }` | `true` |
| `IsApproved == true` | `{ IsApproved: false }` | `false` |
| `!(Status == 'Closed')` | `{ Status: "Open" }` | `true` |

### Transformation Expressions (Value)

Used in `Execute()` to compute new variable values.

| Expression | State Variables | Result |
|---|---|---|
| `'Approved'` | (any) | `"Approved"` (string literal) |
| `Count + 1` | `{ Count: 5 }` | `6` |
| `Amount * 0.9` | `{ Amount: 100 }` | `90.0` |
| `true` | (any) | `true` (literal) |
| `(Price * Quantity) - Discount` | `{ Price: 10, Quantity: 3, Discount: 5 }` | `25` |

**Important:** String literals in expressions must use **single quotes** (`'Approved'`). Unquoted text like `Approved` is interpreted as a variable name reference, which will cause an error if no variable with that name exists in the state.

## Supported Operators

The expression evaluator supports the following operators via NCalc:

**Comparison Operators:**
- Equality: `==`, `!=`
- Numeric: `<`, `>`, `<=`, `>=`

**Logical Operators:**
- AND: `&&`
- OR: `||`
- NOT: `!`

**Arithmetic Operators:**
- Addition: `+`
- Subtraction: `-`
- Multiplication: `*`
- Division: `/`
- Modulo: `%`

**Grouping:**
- Parenthetical expressions: `(Amount + Tax) * Rate`

**Literal Values:**
- Strings: `'Pending'`, `'Approved'` (must use single quotes)
- Integers: `42`, `0`, `-1`
- Booleans: `true`, `false`
- Floats: `3.14`, `0.5`

## Variable Resolution

### How Variables Are Resolved

1. Expression evaluator receives the expression string and the state's variable dictionary
2. Variable names in the expression are matched against dictionary keys
3. Matching is **case-sensitive** and **exact** (`OrderStatus` ≠ `orderstatus`)
4. The variable's current value is substituted into the expression
5. The expression is then evaluated with the substituted values

### Example Resolution

Expression: `"Amount < 1000 && Status == 'Pending'"`
State: `{ Amount: 500, Status: "Pending", IsActive: true }`

Step 1: Resolve `Amount` → `500`
Step 2: Resolve `Status` → `"Pending"`
Step 3: Evaluate `500 < 1000 && "Pending" == "Pending"` → `true`

### Undefined Variables

If an expression references a variable not present in the state, the evaluator should throw an exception with a clear message:
- `"Variable 'MissingVar' not found in state"`

## Implementation

The expression evaluator uses **NCalc**, a lightweight expression evaluator for .NET. NCalc was chosen for its simple API, sandboxed execution (no arbitrary code), minimal dependencies, and .NET 6.0+ support.

### ExpressionEvaluator Class

```csharp
public class ExpressionEvaluator : IExpressionEvaluator
{
    public bool EvaluateBoolean(string expression, Dictionary<string, object> variables)
    {
        var result = Evaluate(expression, variables);
        if (result is bool boolResult)
            return boolResult;
        throw new InvalidOperationException(
            $"Expression '{expression}' did not evaluate to a boolean value.");
    }

    public object Evaluate(string expression, Dictionary<string, object> variables)
    {
        var ncalcExpr = new Expression(expression, ExpressionOptions.NoCache);

        // Set parameters from state variables
        foreach (var kvp in variables)
            ncalcExpr.Parameters[kvp.Key] = kvp.Value;

        return ncalcExpr.Evaluate();
    }
}
```

The evaluator also handles division by zero (NCalc returns `Infinity` instead of throwing) and provides clear error messages for undefined variables and invalid syntax.

## Security

### Sandboxing Requirements

- Expressions must NOT be able to:
  - Access the file system
  - Make network calls
  - Execute arbitrary C# code
  - Access reflection or System namespaces
  - Modify global state

- Expressions CAN only:
  - Read state variables passed to them
  - Perform arithmetic and comparisons
  - Return primitive values

### Input Validation

- Expression strings are validated at evaluation time (not at load time)
- NCalc's `HasErrors()` method is checked before evaluation to catch syntax errors early

## Error Handling

All errors are thrown as `InvalidOperationException` with descriptive messages:

| Error | Message Pattern |
|---|---|
| Undefined parameter | `"Undefined parameter in expression '{expr}'. If you intended a string literal, wrap it in single quotes (e.g. 'value' instead of value). Detail: {message}"` |
| Non-boolean condition | `"Expression '{expr}' did not evaluate to a boolean value. Got: {type}"` |
| Division by zero | `"Division by zero in expression '{expr}'"` |
| Invalid syntax | `"Invalid expression syntax: {details}"` |

## Testing Considerations

- Test each operator type independently
- Test variable resolution with all primitive types (string, int, bool, float)
- Test compound expressions (multiple operators)
- Test edge cases: empty strings, zero values, negative numbers
- Test error cases: undefined variables, type mismatches, invalid syntax
- Test that expressions cannot escape the sandbox

## Related Documentation

- [Declarative Rules Architecture](./declarative-rules.md)
- [State Machine Builder Architecture](./builder-architecture.md)
- [State Immutability](./state-immutability.md)

## References

- PRD Section: Declarative Rule Definition (FR 35-48)
- PRD Section: Expression Evaluation (Design Considerations)
