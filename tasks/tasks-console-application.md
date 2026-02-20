## Relevant Files

- `StateMaker.sln` - Solution file, needs new console project added
- `src/StateMaker.Console/StateMaker.Console.csproj` - New console application project
- `src/StateMaker.Console/Program.cs` - Entry point with argument parsing and command routing
- `src/StateMaker.Console/BuildCommand.cs` - Handles the `build` command: parse definition file, build state machine, export
- `src/StateMaker.Console/ExportCommand.cs` - Handles the `export` command: load JSON state machine, re-export to specified format
- `src/StateMaker.Console/HelpPrinter.cs` - Prints help content to stdout
- `src/StateMaker.Console/BuildDefinitionLoader.cs` - Parses the combined build definition JSON file (initialState + rules + config)
- `src/StateMaker.Console/ExporterFactory.cs` - Maps format strings to IStateMachineExporter instances
- `src/StateMaker/RuleFileLoader.cs` - Existing rule file loader, may need minor adjustments to support the combined format
- `src/StateMaker/BuilderConfig.cs` - Existing builder configuration class
- `src/StateMaker/IStateMachineExporter.cs` - Existing exporter interface
- `src/StateMaker/IStateMachineImporter.cs` - Existing importer interface
- `src/StateMaker/JsonExporter.cs` - Existing JSON exporter
- `src/StateMaker/DotExporter.cs` - Existing DOT exporter
- `src/StateMaker/GraphMlExporter.cs` - Existing GraphML exporter
- `src/StateMaker/JsonImporter.cs` - Existing JSON importer
- `samples/simple-build.json` - Sample build definition file for manual testing

### Notes

- The console project references the `StateMaker` library project for all state machine functionality.
- Use `dotnet test` to run existing library tests and ensure no regressions.
- Use `dotnet run --project src/StateMaker.Console` to test the console app manually.

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
  - [x] 0.1 Create and checkout a new branch for this feature (e.g., `git checkout -b consoleapp`)
- [x] 1.0 Set up the console application project
  - [x] 1.1 Create new console project directory `src/StateMaker.Console`
  - [x] 1.2 Create `StateMaker.Console.csproj` targeting `net6.0` as an executable, referencing the `StateMaker` library project
  - [x] 1.3 Add the new project to `StateMaker.sln`
  - [x] 1.4 Verify the solution builds successfully with `dotnet build`
- [x] 2.0 Implement the build definition file parser
  - [x] 2.1 Create `BuildDefinitionLoader.cs` that parses a combined JSON file containing `initialState`, `rules`, and optional `config` sections
  - [x] 2.2 Parse the `initialState` section into a `State` object (reuse `RuleFileLoader` logic or call its methods)
  - [x] 2.3 Parse the `rules` section into an `IRule[]` array (reuse `RuleFileLoader` logic or call its methods)
  - [x] 2.4 Parse the optional `config` section into a `BuilderConfig` object (handle `maxStates`, `maxDepth`, `explorationStrategy` fields)
  - [x] 2.5 Return a result containing the initial state, rules, and config; use sensible defaults when config is omitted
  - [x] 2.6 Verify the build definition loader works by building the project
- [x] 3.0 Implement the ExporterFactory
  - [x] 3.1 Create `ExporterFactory.cs` that maps format strings (`json`, `dot`, `graphml`) to the corresponding `IStateMachineExporter` implementations
  - [x] 3.2 Throw a clear error for unrecognized format strings
- [x] 4.0 Implement the build command
  - [x] 4.1 Create `BuildCommand.cs` that accepts a definition file path, optional output path, and optional format string
  - [x] 4.2 Use `BuildDefinitionLoader` to parse the definition file
  - [x] 4.3 Use `StateMachineBuilder.Build()` to build the state machine from the parsed initial state, rules, and config
  - [x] 4.4 Use `ExporterFactory` to get the appropriate exporter and export the state machine
  - [x] 4.5 Write the exported content to the output file if `--output` is specified, otherwise write to stdout
- [x] 5.0 Implement the export command
  - [x] 5.1 Create `ExportCommand.cs` that accepts a JSON state machine file path, required format string, and optional output path
  - [x] 5.2 Read the input file and use `JsonImporter` to load the state machine
  - [x] 5.3 Use `ExporterFactory` to get the appropriate exporter and export the state machine
  - [x] 5.4 Write the exported content to the output file if `--output` is specified, otherwise write to stdout
- [x] 6.0 Implement help content and argument routing
  - [x] 6.1 Create `HelpPrinter.cs` that prints usage syntax, command descriptions, options, and examples to stdout
  - [x] 6.2 Implement `Program.cs` with argument parsing that routes to `build`, `export`, or help based on the first argument
  - [x] 6.3 Parse `--output`/`-o` and `--format`/`-f` flags from the argument array
  - [x] 6.4 Print help when no arguments are provided or when an unrecognized command is given
- [x] 7.0 Implement error handling
  - [x] 7.1 Wrap command execution in try/catch in `Program.cs`
  - [x] 7.2 On error, print the exception message and full stack trace to stderr
  - [x] 7.3 Return exit code 1 on error, exit code 0 on success
  - [x] 7.4 Handle specific error cases: missing file path argument, file not found, invalid JSON, unsupported format
- [x] 8.0 End-to-end testing with sample definition files
  - [x] 8.1 Create a sample build definition file (`samples/simple-build.json`) with a small state machine definition
  - [x] 8.2 Test the `build` command: build from definition file with default format (JSON), verify output
  - [x] 8.3 Test the `build` command with `--format dot` and `--format graphml`, verify output
  - [x] 8.4 Test the `build` command with `--output` flag, verify file is created
  - [x] 8.5 Test the `export` command: export a JSON state machine to DOT and GraphML formats
  - [x] 8.6 Test running with no arguments, verify help content is displayed
  - [x] 8.7 Test error cases: missing file, invalid JSON, bad format string
  - [x] 8.8 Verify all existing library tests still pass with `dotnet test`
