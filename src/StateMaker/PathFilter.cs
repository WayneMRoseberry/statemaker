namespace StateMaker;

public class PathFilter
{
    private readonly StateMachine _stateMachine;
    private readonly HashSet<string> _selectedStateIds;

    public PathFilter(StateMachine stateMachine, HashSet<string> selectedStateIds)
    {
        _stateMachine = stateMachine;
        _selectedStateIds = selectedStateIds;
    }

    public StateMachine Filter()
    {
        if (_selectedStateIds.Count == 0 || _stateMachine.StartingStateId is null)
            return new StateMachine();

        // Build forward and reverse adjacency lists
        var forwardAdj = new Dictionary<string, List<Transition>>();
        var reverseAdj = new Dictionary<string, List<Transition>>();
        foreach (var transition in _stateMachine.Transitions)
        {
            if (!forwardAdj.ContainsKey(transition.SourceStateId))
                forwardAdj[transition.SourceStateId] = new List<Transition>();
            forwardAdj[transition.SourceStateId].Add(transition);

            if (!reverseAdj.ContainsKey(transition.TargetStateId))
                reverseAdj[transition.TargetStateId] = new List<Transition>();
            reverseAdj[transition.TargetStateId].Add(transition);
        }

        // Forward BFS from starting state, stopping at selected states
        var forwardReachable = new HashSet<string>();
        var reachedSelected = new HashSet<string>();
        var queue = new Queue<string>();

        queue.Enqueue(_stateMachine.StartingStateId);
        forwardReachable.Add(_stateMachine.StartingStateId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (_selectedStateIds.Contains(current))
            {
                reachedSelected.Add(current);
                continue;
            }

            if (!forwardAdj.TryGetValue(current, out var transitions))
                continue;

            foreach (var transition in transitions)
            {
                if (forwardReachable.Add(transition.TargetStateId))
                {
                    queue.Enqueue(transition.TargetStateId);
                }
            }
        }

        if (reachedSelected.Count == 0)
            return new StateMachine();

        // Reverse BFS from reached selected states, only visiting forward-reachable states
        var pathStates = new HashSet<string>();
        queue.Clear();

        foreach (var selectedId in reachedSelected)
        {
            if (pathStates.Add(selectedId))
                queue.Enqueue(selectedId);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!reverseAdj.TryGetValue(current, out var reverseTransitions))
                continue;

            foreach (var transition in reverseTransitions)
            {
                var predecessor = transition.SourceStateId;
                if (forwardReachable.Contains(predecessor)
                    && !_selectedStateIds.Contains(predecessor)
                    && pathStates.Add(predecessor))
                {
                    queue.Enqueue(predecessor);
                }
            }
        }

        // Build result machine with states on path and their connecting transitions
        var result = new StateMachine();
        foreach (var stateId in pathStates)
        {
            result.AddOrUpdateState(stateId, _stateMachine.States[stateId]);
        }
        result.StartingStateId = _stateMachine.StartingStateId;

        foreach (var transition in _stateMachine.Transitions)
        {
            if (pathStates.Contains(transition.SourceStateId)
                && pathStates.Contains(transition.TargetStateId)
                && !_selectedStateIds.Contains(transition.SourceStateId))
            {
                result.Transitions.Add(transition);
            }
        }

        return result;
    }
}
