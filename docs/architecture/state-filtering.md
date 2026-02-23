# State Filtering Architecture

## Overview

StateMaker supports filtering state machines to find states matching specific conditions, apply attributes to those states, and optionally extract only the paths leading to matched states. This enables focused analysis of large state machines by narrowing the view to relevant portions.

The filtering pipeline consists of three stages:
1. **Filter Definition** - JSON-based rules defining conditions and attributes
2. **Filter Engine** - Evaluates rules against state machine states
3. **Path Traversal** - Extracts reachable paths to matched states

## Filter Definition Format

Filter definitions are JSON files containing an array of filter rules. Each rule has a `condition` expression and optional `attributes` to apply to matching states.

```json
{
  "filters": [
    {
      "condition": "Status == 'Error' && RetryCount > 3",
      "attributes": {
        "highlight": "red",
        "category": "failure"
      }
    },
    {
      "condition": "_stateId == 'S0'",
      "attributes": {
        "role": "entry"
      }
    }
  ]
}
```

### Filter Rule Properties

| Property | Required | Description |
|----------|----------|-------------|
| `condition` | Yes | A boolean expression evaluated against the state's variables. Uses the same NCalc expression syntax as build rules. |
| `attributes` | No | Key-value pairs to apply to matching states. Defaults to empty if omitted. |

### Reserved Variable: `_stateId`

The special variable `_stateId` is injected during evaluation and contains the state's ID string (e.g., `"S0"`, `"S1"`). This allows conditions to match states by their ID rather than by their variable values.

### Loading

`FilterDefinitionLoader` is a static class that loads filter definitions from files or JSON strings. It follows the same patterns as `BuildDefinitionLoader` and `RuleFileLoader`:

- `LoadFromFile(string path)` - Reads and parses a filter definition file
- `LoadFromJson(string json)` - Parses a filter definition from a JSON string
- Invalid JSON throws `JsonParseException`
- Missing `condition` field throws a validation error

## Filter Engine

`FilterEngine` evaluates a `FilterDefinition` against a `StateMachine` using the existing `IExpressionEvaluator` (NCalc-based expression evaluation).

### Evaluation Process

1. For each state in the state machine:
   - Build a variable dictionary from the state's variables
   - Inject `_stateId` with the state's ID
   - For each filter rule, evaluate the condition
   - If the condition is `true`, add the state ID to the selected set and merge the rule's attributes into the state's `Attributes` dictionary
2. Return a `FilterResult` containing:
   - The set of selected state IDs
   - The state machine (with attributes applied to matched states)

### Attribute Merging

When multiple rules match the same state, attributes are applied in rule order. Later rules overwrite duplicate attribute keys from earlier rules. This enables layered attribute assignment where general rules run first and specific rules override.

```json
{
  "filters": [
    { "condition": "true", "attributes": { "color": "gray" } },
    { "condition": "Status == 'Error'", "attributes": { "color": "red" } }
  ]
}
```

In this example, all states get `color=gray`, but error states get `color=red` (overwritten by the second rule).

## State Attributes

### Design

The `State` class has two dictionaries:
- `Variables` - State data produced by the state machine builder (part of the model)
- `Attributes` - Metadata applied by filters (annotations layered on top)

Both are `Dictionary<string, object?>` and follow the same patterns.

### Impact on State Identity

**Important:** `Attributes` are included in `State.Equals()` and `State.GetHashCode()`. This means a state before filtering is **not equal** to the same state after filter attributes are applied. Consumers and the filter engine should be aware that applying attributes changes state identity.

This design was chosen so that attributed states are distinguishable from unattributed states in collections like sets and dictionaries. The trade-off is that comparing states across filtering boundaries requires comparing only `Variables` if attribute-independent equality is needed.

### Serialization

In JSON export, attributes appear as a nested `attributes` object within each state:

```json
"S0": {
  "Status": "Pending",
  "Count": 0,
  "attributes": {
    "highlight": "red",
    "category": "failure"
  }
}
```

When no attributes are present, the `attributes` field is omitted for backward compatibility.

In DOT, GraphML, and Mermaid exports, attributes are rendered in node labels below a `---` divider line, visually separating them from variables:

```
S0
Status='Pending'
Count=0
---
highlight='red'
category='failure'
```

When no attributes are present, the divider and attributes section are omitted entirely.

## Path Traversal

`PathFilter` implements forward reachability from the starting state to any selected (matched) state.

### Algorithm

1. Starting from the state machine's starting state, perform a breadth-first search
2. At each state, follow outgoing transitions to discover connected states
3. Track which states lie on any path from the starting state to a selected state
4. Produce a new `StateMachine` containing only the states on those paths and their connecting transitions

### Behavior

- The starting state is always included if any path exists to a selected state
- States not on any path from the starting state to a selected state are excluded
- If no states match the filter, an empty state machine is returned (no states, no transitions)
- Cycles are handled correctly — visited states are tracked to prevent infinite loops

## Console Commands

### `filter` command

```
statemaker filter <state-machine-file> <filter-file> [options]
```

Loads a state machine, applies filter rules, performs path traversal, and exports the result.

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--format` | `-f` | Output format: `json`, `dot`, `graphml`, `mermaid` | `json` |
| `--output` | `-o` | Output file path | stdout |
| `--list` | | Output matching states as a JSON array (no path traversal) | off |

### `--list` flag

When `--list` is provided, the filter command skips path traversal and outputs a JSON array of matching state definitions:

```json
[
  {
    "stateId": "S2",
    "variables": {
      "Status": "Error",
      "RetryCount": 5
    },
    "attributes": {
      "highlight": "red"
    }
  }
]
```

Each element includes `stateId`, `variables`, and `attributes`. This is useful for querying which states match a filter without producing a full state machine subgraph.

### `--filter` option on `export`

```
statemaker export <state-machine-file> --filter <filter-file> [options]
```

The `export` command accepts an optional `--filter` flag. When provided, it applies the filter engine and path traversal before exporting, combining filtering and export in a single step.

## Related Documentation

- [Export Formats Architecture](./export-formats.md)
- [Expression Evaluation](./expression-evaluation.md)
- [Builder Architecture](./builder-architecture.md)
