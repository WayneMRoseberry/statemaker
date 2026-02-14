# Declarative Rules Architecture

## Overview

This document explains how the StateMaker library supports declarative rule building, allowing users to define state machine rules without writing C# code. The architecture uses polymorphism and interface-based design to treat declarative and custom code-based rules identically.

## Core Architecture Principle

**Key Insight:** Declarative rules implement the same `IRule` interface as custom code-based rules, making them indistinguishable to the builder.

## Component Architecture

### 1. The Common Interface

All rules in StateMaker implement the `IRule` interface:

```csharp
namespace StateMaker
{
    public interface IRule
    {
        bool IsAvailable(State state);
        State Execute(State state);
        string GetName();  // Default returns GetType().Name; override for custom name
    }
}
```

This three-method interface is the foundation for both custom and declarative rules. The `GetName()` method provides the rule name used in transitions; by default it returns the type name, but implementers can override it.

### 2. Custom Code-Based Rules

Traditional approach where developers write C# logic:

```csharp
public class AddOptionRule : IRule
{
    public bool IsAvailable(State state)
    {
        // Developer writes C# logic
        if (!state.Variables.ContainsKey("OptionList"))
            return false;

        var optionList = state.Variables["OptionList"] as List<string>;
        return optionList != null && optionList.Count > 0;
    }

    public State Execute(State state)
    {
        // Developer writes C# logic to create new state
        var newState = state.Clone();
        var optionList = newState.Variables["OptionList"] as List<string>;
        optionList.Add("NewOption");
        return newState;
    }

    public string GetName()
    {
        // Default: return type name. Override for a custom name.
        return GetType().Name;
    }
}
```

### 3. Declarative Rules Implementation

The `DeclarativeRule` class implements `IRule` using expression evaluation instead of hard-coded logic:

```csharp
namespace StateMaker
{
    public class DeclarativeRule : IRule
    {
        private readonly string _name;
        private readonly string _conditionExpression;
        private readonly Dictionary<string, string> _transformations;
        private readonly IExpressionEvaluator _evaluator;

        public DeclarativeRule(
            string name,
            string condition,
            Dictionary<string, string> transformations,
            IExpressionEvaluator evaluator)
        {
            _name = name;
            _conditionExpression = condition;
            _transformations = transformations;
            _evaluator = evaluator;
        }

        public bool IsAvailable(State state)
        {
            // Evaluate the condition expression against the state
            // e.g., "OrderStatus == 'Pending' && Amount < 1000"
            return _evaluator.EvaluateBoolean(_conditionExpression, state.Variables);
        }

        public State Execute(State state)
        {
            // Create new state (immutability requirement)
            var newState = state.Clone();

            // Apply each transformation
            // e.g., "OrderStatus" = "Approved"
            foreach (var transform in _transformations)
            {
                string variableName = transform.Key;
                string valueExpression = transform.Value;

                // Evaluate the expression to get the new value
                object newValue = _evaluator.Evaluate(valueExpression, state.Variables);
                newState.Variables[variableName] = newValue;
            }

            return newState;
        }

        public string GetName()
        {
            // Returns the declarative rule's configured name
            return _name;
        }
    }
}
```

### 4. JSON Definition Format

Users define declarative rules in JSON files:

```json
{
  "rules": [
    {
      "name": "ApproveOrder",
      "condition": "OrderStatus == 'Pending' && Amount < 1000",
      "transformations": {
        "OrderStatus": "Approved",
        "ApprovedCount": "ApprovedCount + 1"
      }
    },
    {
      "name": "RejectOrder",
      "condition": "OrderStatus == 'Pending' && Amount >= 1000",
      "transformations": {
        "OrderStatus": "Rejected",
        "RejectedCount": "RejectedCount + 1"
      }
    }
  ]
}
```

### 5. File Loader Component

The `RuleFileLoader` reads JSON and creates `DeclarativeRule` instances:

```csharp
public class RuleFileLoader
{
    private readonly IExpressionEvaluator _evaluator;

    public IRule[] LoadFromFile(string jsonFilePath)
    {
        var json = File.ReadAllText(jsonFilePath);
        var ruleDefinitions = JsonSerializer.Deserialize<RuleDefinitions>(json);

        var rules = new List<IRule>();

        foreach (var ruleDef in ruleDefinitions.Rules)
        {
            var declarativeRule = new DeclarativeRule(
                ruleDef.Name,
                ruleDef.Condition,
                ruleDef.Transformations,
                _evaluator
            );

            rules.Add(declarativeRule); // Returns IRule[]
        }

        return rules.ToArray();
    }
}
```

### 6. Programmatic API

For users who want to define declarative rules in code without JSON files:

```csharp
public class RuleBuilder
{
    private readonly IExpressionEvaluator _evaluator;

    public IRule DefineRule(
        string name,
        string condition,
        Dictionary<string, string> transformations)
    {
        return new DeclarativeRule(name, condition, transformations, _evaluator);
    }
}
```

## Builder Integration

### The StateMachineBuilder

The builder works with `IRule` instances and doesn't distinguish between rule types:

```csharp
public class StateMachineBuilder : IStateMachineBuilder
{
    public StateMachine Build(
        State initialState,
        IRule[] rules,  // Mix of custom and declarative rules
        BuilderConfig config)
    {
        var stateMachine = new StateMachine();
        var visited = new HashSet<State>();
        var queue = new Queue<(string id, State state)>();
        int stateCounter = 0;

        string initialId = $"S{stateCounter++}";
        stateMachine.AddState(initialId, initialState);
        stateMachine.StartingStateId = initialId;
        visited.Add(initialState);
        queue.Enqueue((initialId, initialState));

        while (queue.Count > 0)
        {
            var (currentId, currentState) = queue.Dequeue();

            // Apply ALL rules regardless of type
            foreach (var rule in rules)
            {
                if (rule.IsAvailable(currentState))  // Polymorphism
                {
                    var newState = rule.Execute(currentState);  // Polymorphism

                    if (!visited.Contains(newState))
                    {
                        string newId = $"S{stateCounter++}";
                        stateMachine.AddState(newId, newState);
                        visited.Add(newState);
                        queue.Enqueue((newId, newState));
                    }
                }
            }
        }

        return stateMachine;
    }
}
```

### Mixing Custom and Declarative Rules

This architecture enables powerful composition:

```csharp
// Load declarative rules from file
var loader = new RuleFileLoader();
var declarativeRules = loader.LoadFromFile("rules.json");

// Create custom code-based rules
var customRules = new IRule[]
{
    new AddOptionRule(),
    new ComplexValidationRule()
};

// Combine both types
var allRules = declarativeRules.Concat(customRules).ToArray();

// Build - the builder treats all rules identically
var stateMachine = builder.Build(initialState, allRules, config);
```

## Design Principles

### 1. Interface Segregation
- `IRule` contains only three essential methods (`IsAvailable`, `Execute`, `GetName`)
- Simple contract makes both custom and declarative implementations straightforward

### 2. Polymorphism
- The builder works exclusively with the `IRule` interface
- Runtime behavior varies based on concrete implementation
- No type checking or casting required

### 3. Composition Over Inheritance
- `DeclarativeRule` composes an `IExpressionEvaluator` rather than hard-coding logic
- Allows swapping expression evaluators (NCalc, DynamicExpresso, etc.)

### 4. Immutability
- Both custom and declarative rules must return new `State` objects
- Original states are never modified
- Prevents side effects and enables safe state comparison

### 5. Flexibility
- Users choose the approach that fits their needs:
  - Code-based for complex logic
  - Declarative for simple rules
  - Mixed approach for hybrid scenarios

## Expression Evaluation

The `IExpressionEvaluator` component handles two types of evaluation:

### Condition Evaluation
Evaluates boolean expressions for `IsAvailable()`:
- Input: `"OrderStatus == 'Pending' && Amount < 1000"`
- Output: `true` or `false`

### Transformation Evaluation
Evaluates value expressions for `Execute()`:
- Input: `"ApprovedCount + 1"`
- Output: Computed value using current state variables

### Supported Expression Features

**Phase 1 (Initial Implementation):**
- Comparison operators: `==`, `!=`, `<`, `>`, `<=`, `>=`
- Logical operators: `&&`, `||`, `!`
- Basic arithmetic: `+`, `-`, `*`, `/`
- Parenthetical expressions

**Phase 2 (Future):**
- String manipulation functions
- Math functions (Max, Min, Abs, etc.)
- Type conversions

### Security Considerations
- Expressions must be sandboxed
- No arbitrary code execution allowed
- Expression evaluator libraries (NCalc, DynamicExpresso) provide this safety

## Implementation Requirements

### State Variable References
- Case-sensitive exact name matching
- Example: `OrderStatus` ≠ `orderstatus`

### Error Handling
- Invalid expressions detected at execution time
- Clear error messages for debugging
- Validation happens when rule is evaluated, not when loaded

### Type Support (Initial Version)
State variables support primitive types only:
- `string`
- `int`
- `bool`
- `float`/`double`

## Benefits of This Architecture

1. **Unified Interface:** Single `IRule` interface for all rule types
2. **Runtime Flexibility:** Mix and match rule types without code changes
3. **Testability:** Easy to mock `IRule` for testing
4. **Extensibility:** New rule types can be added by implementing `IRule`
5. **User Choice:** Developers choose code vs. declarative based on complexity
6. **No Code Coupling:** Builder has zero dependencies on concrete rule implementations

## Related Documentation

- [State Machine Builder Architecture](./builder-architecture.md)
- [Expression Evaluation](./expression-evaluation.md)
- [State Immutability](./state-immutability.md)

## References

- PRD Section: Declarative Rule Definition (FR 31-41)
- PRD Section: Custom Rule Implementation (FR 47-51)
- User Story 5: Business Analyst - Declarative Rules
