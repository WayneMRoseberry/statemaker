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
| `"Approved"` | (any) | `"Approved"` (literal) |
| `Count + 1` | `{ Count: 5 }` | `6` |
| `Amount * 0.9` | `{ Amount: 100 }` | `90.0` |
| `true` | (any) | `true` (literal) |
| `(Price * Quantity) - Discount` | `{ Price: 10, Quantity: 3, Discount: 5 }` | `25` |

## Phased Implementation

### Phase 1: Core Operators (Initial Release)

**Comparison Operators:**
- Equality: `==`, `!=`
- Numeric: `<`, `>`, `<=`, `>=`

**Logical Operators:**
- AND: `&&`
- OR: `||`
- NOT: `!`

**Literal Values:**
- Strings: `'Pending'`, `"Approved"`
- Integers: `42`, `0`, `-1`
- Booleans: `true`, `false`
- Floats: `3.14`, `0.5`

### Phase 2: Arithmetic and Grouping

**Arithmetic Operators:**
- Addition: `+`
- Subtraction: `-`
- Multiplication: `*`
- Division: `/`

**Grouping:**
- Parenthetical expressions: `(Amount + Tax) * Rate`

### Phase 3: Functions (Future)

**String Functions:**
- `ToUpper()`, `ToLower()`, `Contains()`, `StartsWith()`

**Math Functions:**
- `Math.Max()`, `Math.Min()`, `Math.Abs()`, `Math.Round()`

**Type Conversions:**
- `ToString()`, `ToInt()`, `ToFloat()`

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

## Implementation Options

### NCalc

Lightweight expression evaluator for .NET.

```csharp
var expression = new Expression("Amount < 1000 && Status == 'Pending'");
expression.Parameters["Amount"] = 500;
expression.Parameters["Status"] = "Pending";
bool result = (bool)expression.Evaluate();
```

**Pros:** Simple API, good performance, well-maintained
**Cons:** Limited string function support

### DynamicExpresso

More powerful expression evaluator with C#-like syntax.

```csharp
var interpreter = new Interpreter();
interpreter.SetVariable("Amount", 500);
interpreter.SetVariable("Status", "Pending");
bool result = interpreter.Eval<bool>("Amount < 1000 && Status == \"Pending\"");
```

**Pros:** Full C# expression support, type-safe
**Cons:** Larger dependency, more complex

### Selection Criteria

The chosen library must:
1. Support all Phase 1 operators
2. Be sandboxed (no arbitrary code execution)
3. Have minimal dependencies
4. Support .NET 6.0+
5. Be actively maintained
6. Have clear error messages for invalid expressions

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
- Maximum expression length should be enforced to prevent abuse
- Nested parentheses depth should have a reasonable limit

## Error Handling

| Error | Message Example |
|---|---|
| Undefined variable | `"Variable 'Foo' not found in state"` |
| Type mismatch | `"Cannot compare string 'abc' with integer 5"` |
| Division by zero | `"Division by zero in expression 'Amount / 0'"` |
| Invalid syntax | `"Invalid expression syntax: unexpected token '&&' at position 5"` |
| Unknown operator | `"Unknown operator '===' in expression"` |

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
