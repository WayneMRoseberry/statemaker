# PRD: StateMaker Console Application

## Introduction/Overview

The StateMaker library currently provides programmatic APIs for building, exporting, and importing state machines. This feature adds a command-line console application (`statemaker.exe`) that exposes these capabilities to users who want to build and export state machines without writing C# code. Users provide a build definition file containing the initial state, rules, and builder configuration, and the console app builds the state machine and outputs it in a specified format.

## Goals

1. Provide a command-line tool that builds state machines from a single definition file.
2. Support outputting built state machines to JSON files.
3. Support loading pre-built state machines from JSON files and exporting them to a user-specified format.
4. Support all current export formats (JSON, DOT, GraphML) and automatically support future formats added to the library.
5. Display help content when the console is run with no arguments.
6. Provide clear, detailed error output (including stack traces) when something goes wrong.

## User Stories

1. **As a user**, I want to run `statemaker.exe` with a build definition file so that I can generate a state machine without writing code.
2. **As a user**, I want to specify the output format on the command line so that I can get the state machine in JSON, DOT, or GraphML format.
3. **As a user**, I want to load a previously built JSON state machine and re-export it to a different format (e.g., DOT for visualization).
4. **As a user**, I want to see help text when I run `statemaker.exe` with no arguments so that I know how to use the tool.
5. **As a user**, I want to see detailed error messages with stack traces when something fails so that I can diagnose problems with my definition files.

## Functional Requirements

### Help / No Arguments

1. When `statemaker.exe` is run with no arguments, it must print help content describing all available commands, options, and usage examples.
2. The help content must include usage syntax, a description of each command, and examples.

### Build Command

3. The application must accept a `build` command that takes a path to a build definition file.
4. The build definition file is a single JSON file that contains:
   - The initial state (variable names and values)
   - The rules (declarative rules with conditions and transformations)
   - The builder configuration (MaxStates, MaxDepth, ExplorationStrategy)
5. The `build` command must accept an `--output` (or `-o`) argument specifying the output file path.
6. The `build` command must accept a `--format` (or `-f`) argument specifying the export format. Supported values: `json`, `dot`, `graphml`. Default: `json`.
7. If `--output` is not specified, the application must write to stdout.

### Export Command

8. The application must accept an `export` command that takes a path to a pre-built state machine JSON file.
9. The `export` command must accept a `--format` (or `-f`) argument specifying the target export format. Supported values: `json`, `dot`, `graphml`.
10. The `export` command must accept an `--output` (or `-o`) argument specifying the output file path.
11. If `--output` is not specified, the application must write to stdout.
12. The `export` command loads the state machine using `JsonImporter` and re-exports it using the exporter matching the specified format.

### Build Definition File Format

13. The build definition file must be a single JSON file containing three top-level sections: `initialState`, `rules`, and `config`.
14. The `initialState` section is an object of variable name/value pairs.
15. The `rules` section is an array of rule definitions, matching the existing `RuleFileLoader` format (declarative rules with name, condition, and transformations; custom rules with type and assembly).
16. The `config` section is an object with optional fields: `maxStates` (integer), `maxDepth` (integer), `explorationStrategy` (string: `"BreadthFirstSearch"` or `"DepthFirstSearch"`).
17. If the `config` section is omitted, sensible defaults are used (no MaxStates limit, no MaxDepth limit, BreadthFirstSearch).

### Error Handling

18. When an error occurs (bad file path, invalid JSON, rule exceptions, missing required arguments), the application must print the error message and full stack trace to stderr.
19. The application must return a non-zero exit code on error.
20. The application must return exit code 0 on success.

## Non-Goals (Out of Scope)

- Interactive mode or REPL for building state machines.
- GUI or web-based interface.
- Watching files for changes and rebuilding automatically.
- Logging configuration from the command line (the console app does not expose the `IStateMachineLogger` interface to the user).
- Custom rule loading via assembly reflection from the console app (only declarative rules are supported in the build definition file for now, unless the `RuleFileLoader` already supports custom rules in which case that support carries over).

## Technical Considerations

- The console application should be a new project in the solution (e.g., `src/StateMaker.Console/StateMaker.Console.csproj`) that references the `StateMaker` library project.
- The project should target `net6.0` and produce an executable.
- Reuse existing classes: `RuleFileLoader` for rule parsing, `StateMachineBuilder` for building, `JsonExporter`/`DotExporter`/`GraphMlExporter` for export, `JsonImporter` for import.
- The build definition file format extends the existing `RuleFileLoader` JSON format by adding `initialState` and `config` sections alongside the existing `rules` array.
- Argument parsing can use a simple hand-rolled parser or a lightweight library — keep dependencies minimal.

## Success Metrics

- The console application can successfully build a state machine from a definition file and output it in all three formats.
- The console application can load a pre-built JSON state machine and re-export it to DOT and GraphML.
- Running with no arguments prints useful help content.
- All error conditions produce detailed output with stack traces and non-zero exit codes.
- Existing library tests continue to pass with no regressions.

## Open Questions

1. Should the console app support a `--quiet` flag to suppress stack traces and only show the error message?
2. Should the output file be overwritten silently if it already exists, or should the app warn/prompt?
3. Should the `build` command support a `--log-level` flag to enable builder logging to stderr during the build process?
