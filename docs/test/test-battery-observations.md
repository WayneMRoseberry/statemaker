# Test Battery Observations

## Intermittent Failure: RunSingle_ReverseGenerated_PassesAllOracles

**Observed:** During a full test suite run on the `feature/path-filter` branch, one instance
of `RunSingle_ReverseGenerated_PassesAllOracles` failed. The failure could not be reproduced
in five consecutive full suite runs or when the test was run in isolation.

**Suspected Cause:** Oracle 6 in `TestBatteryExecutor` checks a performance heuristic —
if `elapsed_ms / state_count > 100ms/state`, the test fails. During parallel test execution,
system load can inflate elapsed time enough to trip this threshold, especially for definitions
that produce very few states (where a small absolute delay causes a large ms/state ratio).

**Status:** Under observation. Failure reasons are now logged via `ITestOutputHelper` so that
if the failure recurs, the exact definition name, failure reason, state count, transition count,
and elapsed time will be captured in test output.

## Potential Mitigations (NOT YET COMMITTED — proposals only)

The following mitigations are being considered. None have been applied because we do not yet
have enough data to confirm the root cause. Apply only after observing and understanding
the failure reason from a recurrence.

1. **Increase the ms/state threshold for battery tests.**
   Pass a higher `msPerStateThreshold` (e.g., 500ms) to `TestBatteryExecutor.Run()` in
   the Theory-based tests. This reduces sensitivity to system load but weakens the
   performance oracle's ability to detect real regressions.

2. **Separate performance-sensitive tests with `[Trait]`.**
   Tag tests that exercise Oracle 6 with `[Trait("Category", "Performance")]` so they can
   be run in isolation (e.g., `dotnet test --filter Category=Performance`) where system load
   is controlled. Other oracle checks would still run in the full suite.

3. **Exclude Oracle 6 from per-definition `RunSingle` tests.**
   Use the `msPerStateThreshold` parameter to effectively disable Oracle 6 in the Theory
   tests (set threshold to `double.MaxValue`), while keeping it active in the `RunAll`
   sweep tests where aggregation smooths out individual timing noise.
