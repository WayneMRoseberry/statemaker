# PRD: Traversal Export Feature

## Introduction/Overview

The Traversal Export feature extends `statemaker` export capabilities by adding a new `ExportType` option to the `export` command. This option allows users to generate traversal-based output that covers the state machine in different ways: state coverage, transition coverage, path coverage, and state-pair coverage. The feature preserves existing default export behavior and supports all supported formats.

## Goals

1. Allow users to specify an `ExportType` when exporting a state machine.
2. Preserve existing default export behavior when `ExportType` is omitted.
3. Support traversal export semantics for all available formats: `json`, `dot`, `graphml`, and `mermaid`.
4. Make JSON export produce a test-case-like traversal schema with meaningful names and IDs.
5. Ensure the export tool remains easy to use with a single `ExportType` flag.

## User Stories

1. As a developer, I want to export a state machine using `ExportType=AllStates` so I get a set of traversals that collectively visit every state at least once.
2. As a QA engineer, I want to export a state machine using `ExportType=AllTransitions` so I can verify that every transition is covered by at least one traversal.
3. As a test author, I want to export a state machine using `ExportType=AllPaths` so I obtain traversals that represent all reachable paths while avoiding infinite loops.
4. As an integration tester, I want to export a state machine using `ExportType=AllStatePairs` so I can review traversal sequences that connect every possible source and target state pair when a path exists.
5. As a regular user, I want the default export behavior to continue working without specifying `ExportType`.

## Functional Requirements

1. The `export` command must accept a new flag named `ExportType`.
2. Valid `ExportType` values must include: `Default`, `AllStates`, `AllTransitions`, `AllPaths`, and `AllStatePairs`.
3. When `ExportType` is not specified, export output must use the `Default` behavior.
4. The export functionality must work for all supported formats: `json`, `dot`, `graphml`, and `mermaid`.
5. For `AllStates`, the system must export a set of traversals that collectively visit each state at least once.
6. For `AllTransitions`, the system must export a set of traversals that collectively traverse each transition at least once.
7. For `AllPaths`, the system must export a set of traversals that collectively cover every possible path through the model, with safeguards against infinite loops.
8. For `AllStatePairs`, the system must export a set of traversals that attempt to cover every source-target state pair where a valid path exists.
9. JSON export for traversal types must represent each traversal as a test-case-style object with fields including `id`, `name`, `description`, and `transitions`.
10. Traversal objects in JSON output must have meaningful names describing the traversal intent.
11. The `Default` export type must preserve existing export behavior for each supported format.
12. User documentation must be updated to include new behavior

## Non-Goals (Out of Scope)

- This feature will not introduce a new UI or web interface.
- This feature will not change the existing `build` command behavior.
- This feature will not create a separate traversal generation tool outside of the existing `export` command.
- This feature will not require new state machine definitions or change the underlying model semantics.

## Design Considerations

- The `ExportType` flag should be documented in CLI help text alongside the existing `--format` option.
- Meaningful traversal names should describe the coverage goal, such as `VisitAllStates` or `CoverTransitionSourceToTarget`.
- For path coverage, the implementation should include loop-detection or visit-limiter logic to prevent unbounded traversal generation.

## Technical Considerations

- The export command should likely use existing traversal logic or a new traversal module to compute the required traversal sets.
- The JSON schema for traversal output should be designed so downstream test-case or automation tooling can easily consume it.
- The `Default` export type should reuse the current serialization path for compatibility.
- Ensure the new `ExportType` flag is accepted in a case-insensitive manner if the CLI parser supports it.

## Success Metrics

- The export command accepts `ExportType` and produces output without errors for all supported formats.
- `ExportType=AllStates` output covers every state at least once.
- `ExportType=AllTransitions` output covers every transition at least once.
- `ExportType=AllPaths` output covers all reachable paths while avoiding infinite loops.
- `ExportType=AllStatePairs` output attempts to include every possible source-target state pair when a path exists.
- JSON traversal exports are readable and test-case-like, with `id`, `name`, `description`, and `transitions` fields.
- Existing exports remain unchanged when `ExportType` is omitted.

## Open Questions

1. Should traversal coverage behavior be validated before export, or should the export always proceed even if full coverage cannot be achieved?
- always produce the output
- in case where coverage is not possible and output is json, amend what and why to the json file in a separate section. If exporting to a visual format, report coverage not achieved to log/console
2. How should traversal sets be sized or limited when the state machine is very large?
- create a configuration maximum traversals setting that defaults to 1024
3. Should `AllPaths` support a maximum path length or loop count parameter in future iterations?
- create a configuration setting for both maximum path length and loop count. Set default to 100 and 2, repectively.
4. Is there a preferred naming convention for traversal IDs beyond simple incremental values?
- all the user to specify a prefix string to which numbers will be appended
