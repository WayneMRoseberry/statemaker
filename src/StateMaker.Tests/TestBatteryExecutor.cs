using System.Diagnostics;
using System.Globalization;

namespace StateMaker.Tests;

public record TestBatteryResult(
    string DefinitionName,
    bool Passed,
    string? FailureReason,
    int StateCount,
    int TransitionCount,
    TimeSpan ElapsedTime);

public static class TestBatteryExecutor
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    private const double DefaultMsPerStateThreshold = 100.0;

    public static TestBatteryResult Run(BuildDefinition definition)
    {
        return Run(definition, DefaultTimeout);
    }

    public static TestBatteryResult Run(BuildDefinition definition, TimeSpan timeout)
    {
        return Run(definition, timeout, DefaultMsPerStateThreshold);
    }

    public static TestBatteryResult Run(BuildDefinition definition, TimeSpan timeout, double msPerStateThreshold)
    {
        var builder = new StateMachineBuilder();
        StateMachine? result = null;
        TimeSpan elapsed = TimeSpan.Zero;

        // Oracle 1 & 2: No crash/exception and no infinite loop (timeout)
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var task = Task.Run(() => builder.Build(definition.InitialState, definition.Rules, definition.Config));
            if (!task.Wait(timeout))
            {
                return new TestBatteryResult(definition.Name, false, "Timeout: Build did not complete within " + timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture) + "s", 0, 0, timeout);
            }
            stopwatch.Stop();
            elapsed = stopwatch.Elapsed;
            result = task.Result;
        }
        catch (AggregateException ex)
        {
            return new TestBatteryResult(definition.Name, false, $"Exception: {ex.InnerException?.Message ?? ex.Message}", 0, 0, elapsed);
        }
        catch (Exception ex)
        {
            return new TestBatteryResult(definition.Name, false, $"Exception: {ex.Message}", 0, 0, elapsed);
        }

        int stateCount = result.States.Count;
        int transitionCount = result.Transitions.Count;

        // Oracle 3: MaxStates respected
        if (definition.Config.MaxStates.HasValue && definition.Config.MaxStates.Value > 0)
        {
            if (stateCount > definition.Config.MaxStates.Value)
            {
                return new TestBatteryResult(definition.Name, false,
                    $"MaxStates violated: {stateCount.ToString(CultureInfo.InvariantCulture)} states exceeds limit of {definition.Config.MaxStates.Value.ToString(CultureInfo.InvariantCulture)}",
                    stateCount, transitionCount, elapsed);
            }
        }

        // Oracle 4: MaxDepth respected (BFS path-length from start)
        int? maxObservedDepth = null;
        if (result.StartingStateId is not null)
        {
            maxObservedDepth = ComputeMaxDepth(result);
        }

        if (definition.Config.MaxDepth.HasValue && definition.Config.MaxDepth.Value > 0 && maxObservedDepth.HasValue)
        {
            if (maxObservedDepth.Value > definition.Config.MaxDepth.Value)
            {
                return new TestBatteryResult(definition.Name, false,
                    $"MaxDepth violated: observed depth {maxObservedDepth.Value.ToString(CultureInfo.InvariantCulture)} exceeds limit of {definition.Config.MaxDepth.Value.ToString(CultureInfo.InvariantCulture)}",
                    stateCount, transitionCount, elapsed);
            }
        }

        // Oracle 5: Valid machine
        if (!result.IsValidMachine())
        {
            return new TestBatteryResult(definition.Name, false, "IsValidMachine() returned false", stateCount, transitionCount, elapsed);
        }

        // Oracle 6: Time-to-size ratio (performance heuristic)
        if (stateCount > 0)
        {
            double msPerState = elapsed.TotalMilliseconds / stateCount;
            if (msPerState > msPerStateThreshold)
            {
                return new TestBatteryResult(definition.Name, false,
                    $"Performance: {msPerState.ToString("F2", CultureInfo.InvariantCulture)}ms/state exceeds threshold of {msPerStateThreshold.ToString("F0", CultureInfo.InvariantCulture)}ms/state",
                    stateCount, transitionCount, elapsed);
            }
        }

        // Oracle 7: Expected shape matching (when specified)
        if (definition.ExpectedShape is not null)
        {
            var expected = definition.ExpectedShape;

            if (expected.ExpectedStateCount.HasValue && stateCount != expected.ExpectedStateCount.Value)
            {
                return new TestBatteryResult(definition.Name, false,
                    $"Shape mismatch: expected {expected.ExpectedStateCount.Value.ToString(CultureInfo.InvariantCulture)} states, got {stateCount.ToString(CultureInfo.InvariantCulture)}",
                    stateCount, transitionCount, elapsed);
            }

            if (expected.ExpectedTransitionCount.HasValue && transitionCount != expected.ExpectedTransitionCount.Value)
            {
                return new TestBatteryResult(definition.Name, false,
                    $"Shape mismatch: expected {expected.ExpectedTransitionCount.Value.ToString(CultureInfo.InvariantCulture)} transitions, got {transitionCount.ToString(CultureInfo.InvariantCulture)}",
                    stateCount, transitionCount, elapsed);
            }

            if (expected.ExpectedMaxDepth.HasValue && maxObservedDepth.HasValue && maxObservedDepth.Value != expected.ExpectedMaxDepth.Value)
            {
                return new TestBatteryResult(definition.Name, false,
                    $"Shape mismatch: expected max depth {expected.ExpectedMaxDepth.Value.ToString(CultureInfo.InvariantCulture)}, got {maxObservedDepth.Value.ToString(CultureInfo.InvariantCulture)}",
                    stateCount, transitionCount, elapsed);
            }
        }

        return new TestBatteryResult(definition.Name, true, null, stateCount, transitionCount, elapsed);
    }

    public static IEnumerable<TestBatteryResult> RunAll(IEnumerable<BuildDefinition> definitions)
    {
        return RunAll(definitions, DefaultTimeout);
    }

    public static IEnumerable<TestBatteryResult> RunAll(IEnumerable<BuildDefinition> definitions, TimeSpan timeout)
    {
        foreach (var definition in definitions)
        {
            yield return Run(definition, timeout);
        }
    }

    internal static int? ComputeMaxDepth(StateMachine machine)
    {
        if (machine.StartingStateId is null || machine.States.Count == 0)
            return null;

        // Build adjacency list from transitions
        var adjacency = new Dictionary<string, List<string>>();
        foreach (var state in machine.States)
        {
            adjacency[state.Key] = new List<string>();
        }
        foreach (var transition in machine.Transitions)
        {
            if (adjacency.TryGetValue(transition.SourceStateId, out var neighbors))
            {
                neighbors.Add(transition.TargetStateId);
            }
        }

        // BFS from starting state to find maximum shortest-path depth
        var visited = new HashSet<string>();
        var queue = new Queue<(string id, int depth)>();
        queue.Enqueue((machine.StartingStateId, 0));
        visited.Add(machine.StartingStateId);
        int maxDepth = 0;

        while (queue.Count > 0)
        {
            var (currentId, currentDepth) = queue.Dequeue();
            if (currentDepth > maxDepth)
                maxDepth = currentDepth;

            foreach (var neighbor in adjacency[currentId])
            {
                if (visited.Add(neighbor))
                {
                    queue.Enqueue((neighbor, currentDepth + 1));
                }
            }
        }

        return maxDepth;
    }
}
