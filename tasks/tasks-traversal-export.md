## Relevant Files

- `src/StateMaker.Console/Program.cs` - CLI entrypoint; add/parse the new `ExportType` flag.
- `src/StateMaker/ExportCommand.cs` - export command logic; pass `ExportType` through to exporter behavior.
- `src/StateMaker/ExporterFactory.cs` - may need updates if exporters are selected or configured differently.
- `src/StateMaker/JsonExporter.cs` - JSON export formatting; add traversal test-case schema support.
- `src/StateMaker/DotExporter.cs` - dot export formatting; extend behavior for traversal output if needed.
- `src/StateMaker/GraphMlExporter.cs` - GraphML export formatting; extend behavior for traversal output if needed.
- `src/StateMaker/MermaidExporter.cs` - Mermaid export formatting; extend behavior for traversal output if needed.
- `src/StateMaker/StateMachine.cs` or new traversal helper classes - implement traversal set generation and coverage algorithms.
- `src/StateMaker.Tests/ExporterTests.cs` - verify export behavior and traversal output formats.
- `src/StateMaker.Tests/StateMachineBuilderTests.cs` - validate traversal coverage generation logic if new helper classes are added.
- `tasks/prd-traversal-export.md` - source PRD used to generate this task list and reference feature requirements.

### Notes

- Keep the existing default export behavior unchanged when `ExportType` is omitted.
- JSON traversal output should use a test-case-like schema with `id`, `name`, `description`, and `transitions`.
- The new feature should support all supported formats: `json`, `dot`, `graphml`, and `mermaid`.
- Include CLI help documentation updates for the new `ExportType` flag.
- Use TDD when implementing all steps. Use a red/green process. Write the test and enough of an implementation so failure is not because methd is not implemented. Observe the test failing and then implement the feature to pass the test.

## Tasks

- [ ] 0.0 Create feature branch
  - [x] 0.1 Create and checkout a new branch for this feature (e.g., `git checkout -b feature/traversal-export`)
- [x] 1.0 Add CLI support for `ExportType`
  - [x] 1.1 Review `src/StateMaker.Console/Program.cs` and current CLI option parsing patterns.
  - [x] 1.2 Add `ExportType` parsing to the export command path.
  - [x] 1.3 Add `ExportType` documentation to CLI help output.
  - [x] 1.4 Add unit tests for parsing the new `ExportType` flag.
- [x] 2.0 Implement traversal coverage export behavior
  - [x] 2.1 Define a new `ExportType` enum or value set with `Default`, `AllStates`, `AllTransitions`, `AllPaths`, and `AllStatePairs`.
  - [x] 2.2 Extend `src/StateMaker/ExportCommand.cs` to accept and propagate `ExportType`.
  - [x] 2.3 Implement traversal generation helpers for each coverage type.
  - [x] 2.4 Ensure default export behavior remains unchanged when `ExportType` is omitted or set to `Default`.
  - [x] 2.5 Add unit tests covering traversal selection and behavior.
- [x] 3.0 Add JSON traversal export schema and formatting
  - [x] 3.1 Define a traversal schema model with `id`, `name`, `description`, and `transitions`.
  - [x] 3.2 Update `src/StateMaker/JsonExporter.cs` to emit traversal objects for traversal export types.
  - [x] 3.3 Add JSON format tests verifying traversal output structure and content.
- [ ] 4.0 Extend existing exporters for traversal outputs
  - [ ] 4.1 Update `src/StateMaker/DotExporter.cs` to support traversal-oriented export behavior if needed.
  - [ ] 4.2 Update `src/StateMaker/GraphMlExporter.cs` to support traversal-oriented export behavior if needed.
  - [ ] 4.3 Update `src/StateMaker/MermaidExporter.cs` to support traversal-oriented export behavior if needed.
  - [ ] 4.4 Add format-specific tests verifying the default and traversal export outputs for supported formats.
- [ ] 5.0 Update documentation and tests
  - [ ] 5.1 Update CLI documentation and help text to describe `ExportType` and its valid values.
  - [ ] 5.2 Update any related README or docs if `export` command usage is documented there.
  - [ ] 5.3 Add or update tests for failure modes and coverage reporting.
  - [ ] 5.4 Run the relevant test suite and confirm no regressions.
