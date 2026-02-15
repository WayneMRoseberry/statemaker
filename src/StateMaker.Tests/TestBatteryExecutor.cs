using System.Globalization;

namespace StateMaker.Tests;

public record TestBatteryResult(
    string DefinitionName,
    bool Passed,
    string? FailureReason,
    int StateCount,
    int TransitionCount);

public static class TestBatteryExecutor
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public static TestBatteryResult Run(BuildDefinition definition)
    {
        return Run(definition, DefaultTimeout);
    }

    public static TestBatteryResult Run(BuildDefinition definition, TimeSpan timeout)
    {
        var builder = new StateMachineBuilder();
        StateMachine? result = null;

        // Oracle 1 & 2: No crash/exception and no infinite loop (timeout)
        try
        {
            var task = Task.Run(() => builder.Build(definition.InitialState, definition.Rules, definition.Config));
            if (!task.Wait(timeout))
            {
                return new TestBatteryResult(definition.Name, false, "Timeout: Build did not complete within " + timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture) + "s", 0, 0);
            }
            result = task.Result;
        }
        catch (AggregateException ex)
        {
            return new TestBatteryResult(definition.Name, false, $"Exception: {ex.InnerException?.Message ?? ex.Message}", 0, 0);
        }
        catch (Exception ex)
        {
            return new TestBatteryResult(definition.Name, false, $"Exception: {ex.Message}", 0, 0);
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
                    stateCount, transitionCount);
            }
        }

        // Oracle 4: MaxDepth respected (BFS path-length from start)
        if (definition.Config.MaxDepth.HasValue && definition.Config.MaxDepth.Value > 0 && result.StartingStateId is not null)
        {
            int? maxObservedDepth = ComputeMaxDepth(result);
            if (maxObservedDepth.HasValue && maxObservedDepth.Value > definition.Config.MaxDepth.Value)
            {
                return new TestBatteryResult(definition.Name, false,
                    $"MaxDepth violated: observed depth {maxObservedDepth.Value.ToString(CultureInfo.InvariantCulture)} exceeds limit of {definition.Config.MaxDepth.Value.ToString(CultureInfo.InvariantCulture)}",
                    stateCount, transitionCount);
            }
        }

        // Oracle 5: Valid machine
        if (!result.IsValidMachine())
        {
            return new TestBatteryResult(definition.Name, false, "IsValidMachine() returned false", stateCount, transitionCount);
        }

        return new TestBatteryResult(definition.Name, true, null, stateCount, transitionCount);
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

    private static int? ComputeMaxDepth(StateMachine machine)
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
