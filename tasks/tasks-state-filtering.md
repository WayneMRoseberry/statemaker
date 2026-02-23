## Relevant Files

- `src/StateMaker/State.cs` - Add `Attributes` dictionary property alongside existing `Variables`
- `src/StateMaker.Tests/StateTests.cs` - Tests for `Attributes` on State (clone, equality, hashing)
- `src/StateMaker/StateMachine.cs` - May need updates if StateMachine validates or copies state attributes
- `src/StateMaker.Tests/StateMachineTests.cs` - Tests for StateMachine with attributed states
- `src/StateMaker/FilterDefinition.cs` - New model class for filter definitions (conditions + attributes)
- `src/StateMaker/FilterDefinitionLoader.cs` - New loader to parse filter definition JSON files
- `src/StateMaker.Tests/FilterDefinitionLoaderTests.cs` - Tests for filter definition loading and validation
- `src/StateMaker/FilterEngine.cs` - New engine that evaluates filter rules against state machine states
- `src/StateMaker.Tests/FilterEngineTests.cs` - Tests for filter evaluation, attribute assignment, multi-rule merging
- `src/StateMaker/PathFilter.cs` - New class for forward reachability path traversal
- `src/StateMaker.Tests/PathFilterTests.cs` - Tests for path traversal with various graph topologies
- `src/StateMaker/JsonExporter.cs` - Update to include attributes in JSON output
- `src/StateMaker/JsonImporter.cs` - Update to read optional attributes from JSON
- `src/StateMaker/DotExporter.cs` - Update to render attributes in node labels
- `src/StateMaker/GraphMlExporter.cs` - Update to render attributes in node labels
- `src/StateMaker/MermaidExporter.cs` - Update to render attributes in node labels
- `src/StateMaker.Tests/ExporterTests.cs` - Tests for attribute rendering in all export formats
- `src/StateMaker/FilterCommand.cs` - New console command for `filter`
- `src/StateMaker/ExportCommand.cs` - Update to support `--filter` option
- `src/StateMaker.Console/Program.cs` - Register `filter` command and update help text
- `src/StateMaker/HelpPrinter.cs` - Update help text for new command and options
- `src/StateMaker.Tests/FilterCommandTests.cs` - Tests for filter command
- `src/StateMaker.Tests/ExportCommandTests.cs` - Tests for export with `--filter`
- `src/StateMaker.Tests/ProgramTests.cs` - Tests for filter command routing
- `src/StateMaker.Tests/HelpPrinterTests.cs` - Tests for updated help text
- `docs/architecture/state-filtering.md` - Architecture documentation for filtering feature
- `docs/statemaker-console-usage.md` - Update console usage docs with filter command

### Notes

- Unit tests are in `src/StateMaker.Tests/` alongside the project convention.
- Use `dotnet test` to run all tests. Use `dotnet test --filter "FullyQualifiedName~ClassName"` to run specific test classes.
- The existing `IExpressionEvaluator` and `ExpressionEvaluator` must be reused for filter condition evaluation.
- The `State` class currently uses `Variables` as a `Dictionary<string, object>`. The new `Attributes` dictionary should follow the same pattern.
- Filter definition JSON loading should follow the same patterns as `BuildDefinitionLoader` and `RuleFileLoader`, including `JsonParseException` for invalid JSON.

## Instructions for Completing Tasks

**IMPORTANT:** As you complete each task, you must check it off in this markdown file by changing `- [ ]` to `- [x]`. This helps track progress and ensures you don't skip any steps.

Example:
- `- [ ] 1.1 Read file` → `- [x] 1.1 Read file` (after completing)

Update the file after completing each sub-task, not just after completing an entire parent task.

**TDD Approach:** This project follows Test-Driven Development. When starting any new task or behavior change, follow the Red-Green-Refactor cycle:

1. **Red (failing test):** Write the unit tests first, then create a **stub implementation** of the method under test (e.g., returning a default value, throwing `NotImplementedException`, or providing an obviously incomplete body). Run the tests and confirm they **fail due to insufficient functionality** — not merely due to a missing method or compilation error. This step validates that the tests are actually verifying meaningful behavior.
2. **Green (make it pass):** Write the minimum correct implementation to make all tests pass.
3. **Refactor:** Clean up the implementation while keeping all tests green.

All tests must pass before moving on to the next sub-task.

## Tasks

- [x] 0.0 Create feature branch
  - [x] 0.1 Create and checkout a new branch for this feature (e.g., `git checkout -b feature/state-filtering`)

- [x] 1.0 Add `Attributes` dictionary to the `State` class and update serialization
  - [x] 1.1 Add an `Attributes` property (`Dictionary<string, object>`) to the `State` class, initialized to an empty dictionary
  - [x] 1.2 Update `State.Clone()` to deep-copy the `Attributes` dictionary
  - [x] 1.3 Update `State.Equals()` and `State.GetHashCode()` to include `Attributes`
  - [x] 1.4 Update `JsonExporter` to include `attributes` as a separate field in JSON output (omit or output empty object when no attributes)
  - [x] 1.5 Update `JsonImporter` to read the optional `attributes` field from JSON (default to empty dictionary if missing, for backward compatibility)
  - [x] 1.6 Add tests in `StateTests.cs` for clone, equality, and hashing with attributes
  - [x] 1.7 Add tests in `ExporterTests.cs` for JSON round-trip with attributes
  - [x] 1.8 Run all tests and confirm no regressions from the `State` class changes

- [x] 2.0 Create filter definition model and loader
  - [x] 2.1 Create `FilterRule` model class with `Condition` (string) and `Attributes` (dictionary) properties
  - [x] 2.2 Create `FilterDefinition` model class containing a list of `FilterRule` objects
  - [x] 2.3 Create `FilterDefinitionLoader` class with `LoadFromFile(string path)` and `LoadFromJson(string json)` methods, following the same patterns as `BuildDefinitionLoader` and `RuleFileLoader`
  - [x] 2.4 Handle invalid JSON with `JsonParseException`, validate required fields (e.g., `condition` must be present)
  - [x] 2.5 Support requirement 4.1.6: allow `condition` to refer to the state ID (add a reserved variable name like `_stateId` that gets injected during evaluation)
  - [x] 2.6 Add tests in `FilterDefinitionLoaderTests.cs` for valid definitions, missing fields, invalid JSON, multiple rules, and empty filters
  - [x] 2.7 Run tests and confirm all pass

- [x] 3.0 Create filter engine (evaluate filter rules against state machine states)
  - [x] 3.1 Create `FilterEngine` class that accepts a `StateMachine`, a `FilterDefinition`, and an `IExpressionEvaluator`
  - [x] 3.2 Implement evaluation: iterate all states, evaluate each filter rule condition against the state's variables, collect matching states
  - [x] 3.3 Inject the state ID as a reserved variable (e.g., `_stateId`) so conditions can reference it per requirement 4.1.6
  - [x] 3.4 Implement attribute assignment: add each matching rule's attributes to the state's `Attributes` dictionary, with later rules overwriting duplicate keys
  - [x] 3.5 Return the set of selected state IDs (and the state machine with attributes applied)
  - [x] 3.6 Add tests in `FilterEngineTests.cs`: single rule match, no matches, multiple rules with attribute merging, condition referencing state ID, expression evaluation errors
  - [x] 3.7 Run tests and confirm all pass

- [x] 4.0 Create path traversal filter (forward reachability from starting state to selected states)
  - [x] 4.1 Create `PathFilter` class that accepts a `StateMachine` and a set of selected state IDs
  - [x] 4.2 Implement forward reachability: find all paths from starting state to any selected state using BFS/DFS
  - [x] 4.3 Produce a new `StateMachine` containing only the states on those paths and their connecting transitions
  - [x] 4.4 Always include the starting state if any path exists to a selected state
  - [x] 4.5 Return an empty state machine (no states, no transitions) if no states match the filter
  - [x] 4.6 Add tests in `PathFilterTests.cs`: linear chain, branching paths, cycles, no matches yields empty machine, starting state inclusion, states not on path excluded
  - [x] 4.7 Run tests and confirm all pass

- [x] 5.0 Update exporters to render attributes (visually distinguished from variables)
  - [x] 5.1 Update `DotExporter` to include attributes in node labels, visually separated from variables (e.g., with a divider line or prefix)
  - [x] 5.2 Update `MermaidExporter` to include attributes in node labels, visually separated from variables
  - [x] 5.3 Update `GraphMlExporter` to include attributes in node labels, visually separated from variables
  - [x] 5.4 Ensure exporters handle states with no attributes gracefully (no divider or extra whitespace)
  - [x] 5.5 Add tests in `ExporterTests.cs` for each exporter with states that have attributes and states without attributes
  - [x] 5.6 Run tests and confirm all pass

- [x] 6.0 Add `filter` console command and `--filter` option on `export` command
  - [x] 6.1 Create `FilterCommand` class following the pattern of `BuildCommand` and `ExportCommand`: load state machine, load filter definition, run filter engine, run path traversal, export result
  - [x] 6.2 Update `ExportCommand` to accept an optional `--filter` argument; when provided, apply filter engine and path traversal before exporting
  - [x] 6.3 Update `Program.cs` to route the `filter` command to `FilterCommand`
  - [x] 6.4 Update `HelpPrinter` with usage text for the `filter` command and `--filter` option on `export`
  - [x] 6.5 Add tests in `FilterCommandTests.cs` for successful filtering, missing files, invalid filter definition, and output format options
  - [x] 6.6 Add tests in `ExportCommandTests.cs` for the `--filter` option
  - [x] 6.7 Update `ProgramTests.cs` for filter command routing
  - [x] 6.8 Update `HelpPrinterTests.cs` for new help text
  - [x] 6.9 Run tests and confirm all pass

- [ ] 7.0 Add `--list` flag support to the `filter` command
  - [ ] 7.1 Update `FilterCommand` to accept a `--list` flag
  - [ ] 7.2 When `--list` is provided, output a JSON array of full state definitions (with variables and attributes) for matching states, without path traversal
  - [ ] 7.3 Add tests in `FilterCommandTests.cs` for `--list` flag output format and content
  - [ ] 7.4 Update `HelpPrinter` with `--list` option documentation
  - [ ] 7.5 Run tests and confirm all pass

- [ ] 8.0 Update documentation
  - [ ] 8.1 Create `docs/architecture/state-filtering.md` covering filter definition format, filter engine, path traversal algorithm, and attribute rendering
  - [ ] 8.2 In `docs/architecture/state-filtering.md`, document that `Attributes` are included in `State.Equals()` and `GetHashCode()`. This means a state before filtering is not equal to the same state after filter attributes are applied. Consumers and the filter engine should be aware that applying attributes changes state identity.
  - [ ] 8.3 Update `docs/statemaker-console-usage.md` with `filter` command usage, `--filter` option on `export`, `--list` flag, and example output
  - [ ] 8.4 Run full test suite and Release build as final verification
