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

Users define declarative rules in JSON files. Rules can be declarative (the default) or custom (loaded from an external assembly). The `type` field is optional for declarative rules.

```json
{
  "initialState": {
    "OrderStatus": "Pending",
    "Amount": 500
  },
  "rules": [
    {
      "name": "ApproveOrder",
      "condition": "OrderStatus == 'Pending' && Amount < 1000",
      "transformations": {
        "OrderStatus": "'Approved'",
        "ApprovedCount": "ApprovedCount + 1"
      }
    },
    {
      "name": "RejectOrder",
      "condition": "OrderStatus == 'Pending' && Amount >= 1000",
      "transformations": {
        "OrderStatus": "'Rejected'"
      }
    },
    {
      "type": "custom",
      "assemblyPath": "path/to/MyRules.dll",
      "className": "MyNamespace.MyCustomRule"
    }
  ],
  "config": {
    "maxStates": 100,
    "explorationStrategy": "BreadthFirstSearch"
  }
}
```

Custom rules loaded from assemblies must implement `IRule` and have a parameterless constructor.

### 5. File Loader Components

Two loader classes handle JSON definition files:

**`RuleFileLoader`** reads JSON and creates rule instances. It returns a tuple of the optional initial state and the parsed rules array:

```csharp
public class RuleFileLoader
{
    private readonly IExpressionEvaluator _evaluator;

    public (State? initialState, IRule[] rules) LoadFromFile(string filePath);
    public (State? initialState, IRule[] rules) LoadFromJson(string json);
}
```

The loader supports both declarative rules (parsed from `name`, `condition`, `transformations` fields) and custom rules (loaded via reflection from `assemblyPath` and `className` fields).

**`BuildDefinitionLoader`** wraps `RuleFileLoader` and adds config parsing for the console application's `build` command:

```csharp
public class BuildDefinitionLoader
{
    public BuildDefinitionResult LoadFromFile(string filePath);
    public BuildDefinitionResult LoadFromJson(string json);
}

public class BuildDefinitionResult
{
    public State InitialState { get; set; }
    public IRule[] Rules { get; set; }
    public BuilderConfig Config { get; set; }
}
```

Both loaders use shared property name constants from `BuildJsonPropertyNames` (e.g., `"initialState"`, `"rules"`, `"config"`).

## Builder Integration

### The StateMachineBuilder

The builder works with `IRule` instances and doesn't distinguish between rule types. It uses a `Dictionary<State, string>` for O(1) duplicate detection and a `LinkedList` as a unified frontier (FIFO for BFS, LIFO for DFS):

```csharp
public class StateMachineBuilder : IStateMachineBuilder
{
    public StateMachine Build(
        State initialState,
        IRule[] rules,  // Mix of custom and declarative rules
        BuilderConfig config)
    {
        var stateMachine = new StateMachine();
        var stateToId = new Dictionary<State, string>();
        var frontier = new LinkedList<(string id, State state, int depth)>();
        int stateCounter = 0;

        string initialId = $"S{stateCounter++}";
        stateMachine.AddOrUpdateState(initialId, initialState);
        stateMachine.StartingStateId = initialId;
        stateToId[initialState] = initialId;
        frontier.AddLast((initialId, initialState, 0));

        while (frontier.Count > 0)
        {
            // BFS takes from front, DFS takes from back
            var (currentId, currentState, currentDepth) = /* ... */;

            // Apply ALL rules regardless of type
            foreach (var rule in rules)
            {
                if (rule.IsAvailable(currentState))  // Polymorphism
                {
                    var newState = rule.Execute(currentState);  // Polymorphism

                    if (stateToId.TryGetValue(newState, out string? existingId))
                    {
                        // Cycle: transition to existing state
                        stateMachine.Transitions.Add(new Transition(currentId, existingId, rule.GetName()));
                    }
                    else
                    {
                        string newId = $"S{stateCounter++}";
                        stateMachine.AddOrUpdateState(newId, newState);
                        stateToId[newState] = newId;
                        stateMachine.Transitions.Add(new Transition(currentId, newId, rule.GetName()));
                        frontier.AddLast((newId, newState, currentDepth + 1));
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
var loader = new RuleFileLoader(new ExpressionEvaluator());
var (initialState, declarativeRules) = loader.LoadFromFile("rules.json");

// Create custom code-based rules
var customRules = new IRule[]
{
    new AddOptionRule(),
    new ComplexValidationRule()
};

// Combine both types
var allRules = declarativeRules.Concat(customRules).ToArray();

// Build - the builder treats all rules identically
var builder = new StateMachineBuilder();
var stateMachine = builder.Build(initialState!, allRules, new BuilderConfig());
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

The expression evaluator (NCalc) supports:
- Comparison operators: `==`, `!=`, `<`, `>`, `<=`, `>=`
- Logical operators: `&&`, `||`, `!`
- Arithmetic operators: `+`, `-`, `*`, `/`, `%`
- Parenthetical expressions: `(Amount + Tax) * Rate`

### Security Considerations
- Expressions are sandboxed via NCalc — no arbitrary code execution
- Expressions can only read state variables and return primitive values

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
