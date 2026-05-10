namespace StateMaker;

public static class TraversalGenerator
{
    public static IReadOnlyList<Traversal> Generate(StateMachine stateMachine, ExportType exportType, string idPrefix = "T")
    {
        ArgumentNullException.ThrowIfNull(stateMachine);
        return exportType switch
        {
            ExportType.AllStates => GenerateAllStates(stateMachine, idPrefix),
            ExportType.AllTransitions => GenerateAllTransitions(stateMachine, idPrefix),
            ExportType.AllPaths => GenerateAllPaths(stateMachine, idPrefix),
            ExportType.AllStatePairs => GenerateAllStatePairs(stateMachine, idPrefix),
            _ => throw new ArgumentException($"ExportType '{exportType}' does not produce traversals.", nameof(exportType))
        };
    }

    private static List<Traversal> GenerateAllStates(StateMachine sm, string idPrefix)
    {
        var startId = sm.StartingStateId ?? throw new InvalidOperationException("State machine has no starting state.");
        var result = new List<Traversal>();
        var pathsFromStart = FindAllShortestPaths(sm, startId);
        int counter = 1;

        foreach (var stateId in sm.States.Keys)
        {
            if (!pathsFromStart.TryGetValue(stateId, out var path)) continue;
            var id = $"{idPrefix}{counter++}";
            result.Add(new Traversal(id, $"Reach {stateId}", $"Traversal that reaches state {stateId}", path));
        }

        return result;
    }

    private static List<Traversal> GenerateAllTransitions(StateMachine sm, string idPrefix)
    {
        var startId = sm.StartingStateId ?? throw new InvalidOperationException("State machine has no starting state.");
        var result = new List<Traversal>();
        var pathsFromStart = FindAllShortestPaths(sm, startId);
        int counter = 1;

        foreach (var transition in sm.Transitions)
        {
            if (!pathsFromStart.TryGetValue(transition.SourceStateId, out var pathToSource)) continue;

            var fullPath = new List<Transition>(pathToSource) { transition };
            var id = $"{idPrefix}{counter++}";
            var name = $"Exercise {transition.SourceStateId} to {transition.TargetStateId} via {transition.RuleName}";
            var desc = $"Traversal exercising the {transition.RuleName} transition from {transition.SourceStateId} to {transition.TargetStateId}";
            result.Add(new Traversal(id, name, desc, fullPath));
        }

        return result;
    }

    private static List<Traversal> GenerateAllPaths(StateMachine sm, string idPrefix, int maxPathLength = 100)
    {
        var startId = sm.StartingStateId ?? throw new InvalidOperationException("State machine has no starting state.");
        var result = new List<Traversal>();
        var visited = new HashSet<string> { startId };

        DfsAllPaths(sm, startId, new List<Transition>(), visited, result, idPrefix, maxPathLength);
        return result;
    }

    private static void DfsAllPaths(StateMachine sm, string currentId, List<Transition> currentPath,
        HashSet<string> visited, List<Traversal> result, string idPrefix, int maxPathLength)
    {
        bool canExtend = false;

        if (currentPath.Count < maxPathLength)
        {
            foreach (var t in sm.Transitions)
            {
                if (t.SourceStateId != currentId) continue;
                if (visited.Contains(t.TargetStateId)) continue;

                canExtend = true;
                currentPath.Add(t);
                visited.Add(t.TargetStateId);
                DfsAllPaths(sm, t.TargetStateId, currentPath, visited, result, idPrefix, maxPathLength);
                currentPath.RemoveAt(currentPath.Count - 1);
                visited.Remove(t.TargetStateId);
            }
        }

        if (!canExtend)
        {
            var n = result.Count + 1;
            var id = $"{idPrefix}{n}";
            result.Add(new Traversal(id, $"Path {n}", $"Path {n} through the state machine", new List<Transition>(currentPath)));
        }
    }

    private static List<Traversal> GenerateAllStatePairs(StateMachine sm, string idPrefix, int maxTraversals = 1024)
    {
        var startId = sm.StartingStateId ?? throw new InvalidOperationException("State machine has no starting state.");
        var result = new List<Traversal>();
        var pathsFromStart = FindAllShortestPaths(sm, startId);
        int counter = 1;

        foreach (var (stateA, pathToA) in pathsFromStart)
        {
            var pathsFromA = FindAllShortestPaths(sm, stateA);

            foreach (var (stateB, pathAtoB) in pathsFromA)
            {
                if (stateB == stateA) continue;
                if (result.Count >= maxTraversals) break;

                var fullPath = new List<Transition>(pathToA.Count + pathAtoB.Count);
                fullPath.AddRange(pathToA);
                fullPath.AddRange(pathAtoB);

                var id = $"{idPrefix}{counter++}";
                result.Add(new Traversal(id, $"From {stateA} to {stateB}",
                    $"Traversal from state {stateA} to state {stateB}", fullPath));
            }

            if (result.Count >= maxTraversals) break;
        }

        return result;
    }

    private static Dictionary<string, IReadOnlyList<Transition>> FindAllShortestPaths(StateMachine sm, string fromId)
    {
        var result = new Dictionary<string, IReadOnlyList<Transition>>
        {
            [fromId] = Array.Empty<Transition>()
        };

        var queue = new Queue<(string stateId, List<Transition> path)>();
        queue.Enqueue((fromId, new List<Transition>()));

        while (queue.Count > 0)
        {
            var (currentId, currentPath) = queue.Dequeue();

            foreach (var t in sm.Transitions)
            {
                if (t.SourceStateId != currentId) continue;
                if (result.ContainsKey(t.TargetStateId)) continue;

                var newPath = new List<Transition>(currentPath) { t };
                result[t.TargetStateId] = newPath;
                queue.Enqueue((t.TargetStateId, newPath));
            }
        }

        return result;
    }
}
