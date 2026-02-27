using System.Globalization;
using System.Text;
using Xunit.Abstractions;

namespace StateMaker.Tests;

/// <summary>
/// Verifies that each genbuild_* file in sampledata produces a state machine
/// semantically equivalent to the corresponding machine_* file.
///
/// Semantic equivalence means:
///   - Same set of distinct state variable-value sets
///   - Same multiset of (source-state-vars, target-state-vars, rule-name) transitions
///
/// State IDs are intentionally ignored — different exploration strategies or rule
/// orderings may assign IDs in different orders while still producing the same graph.
///
/// NOTE: This suite is excluded from CI. Run manually when investigating whether
/// genbuild_* rule definitions correctly reproduce their corresponding machine_* files.
/// To run: dotnet test --filter "GenbuildVerification"
/// </summary>
[Trait("Category", "GenbuildVerification")]
public class GenbuildVerificationTests
{
    private readonly ITestOutputHelper _output;

    public GenbuildVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void AllGenbuildFiles_ProduceSemanticallySameMachineAsCorrespondingMachineFile()
    {
        var sampledataDir = Path.Combine(AppContext.BaseDirectory, "sampledata");

        var genbuildFiles = Directory.GetFiles(sampledataDir, "genbuild_*.json")
            .OrderBy(f => f)
            .ToArray();

        Assert.True(genbuildFiles.Length > 0,
            $"No genbuild_*.json files found in {sampledataDir}");

        var results = new List<(string genbuildName, bool passed, List<string> messages)>();

        foreach (var genbuildFile in genbuildFiles)
        {
            var genbuildName = Path.GetFileName(genbuildFile);
            var machineName = genbuildName.Replace("genbuild_", "machine_", StringComparison.Ordinal);
            var machineFile = Path.Combine(sampledataDir, machineName);
            var messages = new List<string>();

            try
            {
                if (!File.Exists(machineFile))
                {
                    messages.Add($"No corresponding machine file found: {machineName}");
                    results.Add((genbuildName, false, messages));
                    continue;
                }

                // Build machine from genbuild file
                var loader = new BuildDefinitionLoader();
                var buildResult = loader.LoadFromFile(genbuildFile);
                var builder = new StateMachineBuilder();
                var builtMachine = builder.Build(
                    buildResult.InitialState, buildResult.Rules, buildResult.Config);

                // Load expected machine from machine file
                var importer = new JsonImporter();
                var expectedMachine = importer.Import(File.ReadAllText(machineFile));

                // Compare semantically
                var diffs = CompareMachinesSemantically(builtMachine, expectedMachine);
                bool passed = diffs.Count == 0;

                if (!passed)
                    messages.AddRange(diffs);
                else
                    messages.Add($"States: {builtMachine.States.Count}, " +
                                 $"Transitions: {builtMachine.Transitions.Count}");

                results.Add((genbuildName, passed, messages));
            }
            catch (Exception ex)
            {
                messages.Add($"EXCEPTION {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException is not null)
                    messages.Add($"  Inner: {ex.InnerException.Message}");
                results.Add((genbuildName, false, messages));
            }
        }

        // Build report
        var report = new StringBuilder();
        report.AppendLine();
        report.AppendLine("=== Genbuild Verification Report ===");
        report.AppendLine();

        int passCount = results.Count(r => r.passed);
        int failCount = results.Count(r => !r.passed);

        foreach (var (name, passed, messages) in results)
        {
            var status = passed ? "PASS" : "FAIL";
            report.AppendLine(CultureInfo.InvariantCulture, $"[{status}] {name}");
            foreach (var msg in messages)
                report.AppendLine(CultureInfo.InvariantCulture, $"       {msg}");
        }

        report.AppendLine();
        report.AppendLine(CultureInfo.InvariantCulture,
            $"Result: {passCount}/{results.Count} passed, {failCount} failed");

        _output.WriteLine(report.ToString());

        Assert.True(failCount == 0, report.ToString());
    }

    // -------------------------------------------------------------------------
    // Semantic comparison
    // -------------------------------------------------------------------------

    /// <summary>
    /// Compares two state machines semantically: same state value-sets and same
    /// transition graph (ignoring state IDs). Returns a list of human-readable
    /// difference descriptions; empty list means they are equivalent.
    /// </summary>
    private static List<string> CompareMachinesSemantically(
        StateMachine actual, StateMachine expected)
    {
        var diffs = new List<string>();

        // --- Compare state sets ---

        // Represent each state as a canonical string; compare as multisets
        // (states are unique by value in a well-formed machine, but we use multisets
        //  to surface any accidental duplicates clearly).
        var actualStateStrings = actual.States.Values
            .Select(FormatState)
            .OrderBy(s => s)
            .ToList();

        var expectedStateStrings = expected.States.Values
            .Select(FormatState)
            .OrderBy(s => s)
            .ToList();

        var missingStates = MultisetExcept(expectedStateStrings, actualStateStrings);
        var extraStates   = MultisetExcept(actualStateStrings, expectedStateStrings);

        if (actualStateStrings.Count != expectedStateStrings.Count)
            diffs.Add($"State count: built={actualStateStrings.Count}, " +
                      $"expected={expectedStateStrings.Count}");

        foreach (var s in missingStates)
            diffs.Add($"Missing state: {s}");
        foreach (var s in extraStates)
            diffs.Add($"Extra state:   {s}");

        // --- Compare transition multisets ---

        var actualTransStrings  = SemanticTransitionStrings(actual);
        var expectedTransStrings = SemanticTransitionStrings(expected);

        var missingTrans = MultisetExcept(expectedTransStrings, actualTransStrings);
        var extraTrans   = MultisetExcept(actualTransStrings, expectedTransStrings);

        if (actualTransStrings.Count != expectedTransStrings.Count)
            diffs.Add($"Transition count: built={actualTransStrings.Count}, " +
                      $"expected={expectedTransStrings.Count}");

        foreach (var t in missingTrans)
            diffs.Add($"Missing transition: {t}");
        foreach (var t in extraTrans)
            diffs.Add($"Extra transition:   {t}");

        return diffs;
    }

    /// <summary>
    /// Builds a list of canonical transition strings:
    ///   "{source-state} --RuleName--> {target-state}"
    /// using state variable values rather than IDs, so the result is
    /// independent of state ID assignment order.
    /// </summary>
    private static List<string> SemanticTransitionStrings(StateMachine machine)
    {
        return machine.Transitions
            .Select(t =>
            {
                var src = FormatState(machine.States[t.SourceStateId]);
                var tgt = FormatState(machine.States[t.TargetStateId]);
                return $"{src} --{t.RuleName}--> {tgt}";
            })
            .OrderBy(s => s)
            .ToList();
    }

    /// <summary>
    /// Returns elements that are in <paramref name="from"/> but not in
    /// <paramref name="remove"/>, treating both as multisets (each occurrence
    /// is matched independently).
    /// </summary>
    private static List<string> MultisetExcept(List<string> from, List<string> remove)
    {
        var remaining = new List<string>(from);
        foreach (var item in remove)
        {
            int idx = remaining.IndexOf(item);
            if (idx >= 0)
                remaining.RemoveAt(idx);
        }
        return remaining;
    }

    /// <summary>
    /// Produces a deterministic, human-readable representation of a state's
    /// variable values, e.g. "{branch=trunk, step=0}". Variables are sorted by
    /// key so the output is stable regardless of dictionary insertion order.
    /// </summary>
    private static string FormatState(State state)
    {
        var parts = state.Variables
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={FormatValue(kv.Value)}");
        return "{" + string.Join(", ", parts) + "}";
    }

    private static string FormatValue(object? value) => value switch
    {
        null          => "null",
        bool b        => b ? "true" : "false",
        string s      => $"'{s}'",
        _             => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null"
    };
}
