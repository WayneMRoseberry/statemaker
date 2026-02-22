# PRD: State Filtering, Selection, and Attribution

## 1. Introduction/Overview

StateMaker builds state machines from declarative rule definitions and exports them to various formats. Currently, there is no way to query, filter, or annotate the states within a built state machine.

State filtering adds the ability to select states from a built state machine based on rule expressions, attach markup attributes to those states, and export a reduced state machine containing only the traversal paths that lead to the selected states. This enables use cases such as identifying high-value usage paths, marking states for visual distinction in exports, and reducing large state machines to focused subsets.

## 2. Goals

1. Provide a rules-based mechanism for selecting states from a built state machine using expression conditions similar to the existing build rule system.
2. Allow markup attributes to be attached to selected states, stored separately from state machine variables.
3. Enable export of filtered state machines containing only the paths from the starting state to selected states.
4. Integrate filtering into the console application as both a standalone `filter` command and a `--filter` option on the existing `export` command.

## 3. User Stories

- **As a business analyst**, I want to define criteria for high-priority features and export a state machine showing only the paths that lead to those features, so I can understand which sequences of actions reach important outcomes.
- **As a tester**, I want to filter a state machine to states where a specific variable has a certain value, so I can focus test generation on a relevant subset of the system.
- **As a tester**, I want to find ways where the system might get into undesirable states. I define a criteria for undesirable states and apply it to a pre-built state machine defintion to get a list of the traversal patterns which lead to undesirable states.
- **As a developer**, I want to attach ranking or category attributes to filtered states, so that exported diagrams visually distinguish those states from others.
- **As a user**, I want to apply a filter during export from the command line, so I can quickly generate focused diagrams without writing a separate filter definition.

## 4. Functional Requirements

### 4.1 Filter Definition Format

1. Filter definitions must be expressed in a JSON file format similar to build definition rules.
2. Each filter rule must contain a `condition` field using the same expression syntax as build rules (NCalc expressions).
3. Filter rule conditions must be evaluated against the variables of each state in the state machine.
4. A state that satisfies any filter rule condition is considered "selected."
5. Filter definitions must support multiple filter rules, each with its own condition and optional attribute assignments.
6. Filters will allow `condition` to refer to the state ID in cases where someone knows specifically which node they wish to filter to.

### 4.2 State Attributes

6. The `State` class must have a separate `Attributes` dictionary (distinct from `Variables`) for storing markup attributes. This property is optional on imported state machine definition files, as it is generally only applied when processing filters..
7. Filter rules must be able to specify key-value attribute pairs to attach to states that match their condition (e.g., `"attributes": { "ranking": "high" }`).
8. Attribute values must support the same types as state variables: string, number, boolean, and null.
9. Attributes must not interfere with state machine variables or rule evaluation.
10. Attributes must be preserved through export operations.

### 4.3 Filter Evaluation

10. A filter engine must accept a built `StateMachine` and a filter definition, and return which states match the filter criteria.
11. Filter rule conditions must be evaluated using the same `IExpressionEvaluator` used by the build system.
12. States that match a filter rule must have the rule's specified attributes added to their `Attributes` dictionary.
13. If a state matches multiple filter rules, attributes from all matching rules must be merged (later rules overwrite duplicate keys).

### 4.4 Path Traversal Export

14. Given a set of selected (filtered) states, the system must produce a new `StateMachine` containing only states on paths from the starting state to any selected state (forward reachability).
15. The filtered state machine must include all transitions between the retained states.
16. The starting state must always be included if any path exists to a selected state.
17. States that are not on any path from the starting state to a selected state must be excluded.
18. If no states match the filter, the resulting state machine must be empty (no states, no transitions).

### 4.5 Console Integration

19. A new `filter` command must be available: `statemaker.console filter <state-machine-file> <filter-definition-file> [options]`.
20. The `filter` command must accept `--format` and `--output` options matching the existing `build` and `export` commands.
21. The existing `export` command must accept an optional `--filter` option that takes a filter definition file path.
22. When `--filter` is provided to `export`, the export must apply the filter and path traversal before exporting.
23. Both commands must output the filtered state machine in the requested format (json, dot, graphml, mermaid).
24. The `filter` command must accept a `--list` flag that outputs a JSON array of full state definitions for all states matching the filter criteria, without performing path traversal or full state machine export.

### 4.6 Attribute Rendering in Exports

25. Exporters should include state attributes in their output where the format supports it (e.g., as additional label lines, node styling, or metadata).
26. Attributes must be visually distinguished from variables by default in label-based formats (e.g., separated by a divider or prefix).
27. The JSON exporter must include attributes in the JSON output as a separate field from variables.

## 5. Non-Goals (Out of Scope)

- **Backward reachability**: Filtering paths that can reach a selected state from any state (not just from the starting state) is out of scope for this phase.
- **Filter composition**: Combining multiple filter definition files or chaining filters is not included.
- **Interactive filtering**: No interactive or GUI-based filtering interface.
- **Modifying variables**: Filters select and annotate states but do not modify state machine variables.
- **Import of filtered formats**: Importing a filtered state machine back with attribute preservation is not required.
- **Custom attribute rendering**: Users may eventually want custom visual treatment for attributes (different colors, bold text, larger fonts, different node shapes, etc.). This is a future goal but out of scope for this phase. The current design should anticipate this extensibility.

## 6. Design Considerations

### Filter Definition File Example

```json
{
  "filters": [
    {
      "condition": "Status == 'Approved' && Amount > 1000",
      "attributes": {
        "ranking": "high",
        "category": "priority"
      }
    },
    {
      "condition": "IsComplete == true",
      "attributes": {
        "ranking": "low"
      }
    }
  ]
}
```

### State Attributes on State Class

The `State` class gains a new `Attributes` dictionary alongside the existing `Variables` dictionary:

```
State
├── Variables: { "Status": "Approved", "Amount": 1500 }
└── Attributes: { "ranking": "high", "category": "priority" }
```

### Attribute Display in Exports

In label-based formats (DOT, Mermaid, GraphML), attributes could appear as additional lines in the node label, visually separated from variables (e.g., with a divider or prefix).

## 7. Technical Considerations

- The expression evaluation must reuse the existing `IExpressionEvaluator` and `ExpressionEvaluator` implementation.
- Path traversal filtering (forward reachability from starting state to selected nodes) is a graph algorithm — breadth-first or depth-first search from the starting state, retaining only paths that terminate at a selected state.
- The `State` class change (adding `Attributes`) will affect serialization. JSON import/export must handle the new field with backward compatibility (missing `attributes` field defaults to empty dictionary).
- Filter definition loading should follow the same patterns as `BuildDefinitionLoader` and `RuleFileLoader`, including `JsonParseException` for invalid JSON.

## 8. Success Metrics

- All existing tests continue to pass (no regressions from `State` class changes).
- Filter rules correctly select states matching expression conditions.
- Attributes are correctly attached to matching states and preserved in all export formats.
- Path traversal produces correct reduced state machines (verified against known graph structures).
- Console `filter` command and `export --filter` option produce expected output.
- Round-trip: a state machine exported to JSON with attributes can be re-imported with attributes intact.

## 9. Resolved Questions

1. **Attribute value types**: Attribute key-value pairs support the same types as state variables (string, number, boolean, null).
2. **Attribute rendering in exports**: Attributes are visually distinguished from variables by default (e.g., separated by a divider line or prefix). The design should anticipate future extensibility where users could specify custom visual treatment for attributes (different colors, bold, larger text, different node shapes, etc.), but custom rendering is out of scope for this PRD.
3. **Listing matched states**: The `filter` command supports a `--list` flag that outputs a JSON array of full state definitions (not just IDs) for all states matching the filter criteria, without performing path traversal or full state machine export.
