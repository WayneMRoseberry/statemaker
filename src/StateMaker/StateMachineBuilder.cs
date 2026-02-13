namespace StateMaker;

public class StateMachineBuilder : IStateMachineBuilder
{
    public StateMachine Build(State initialState, IRule[] rules, BuilderConfig config)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(config);

        for (int i = 0; i < rules.Length; i++)
        {
            if (rules[i] is null)
                throw new ArgumentNullException($"rules[{i}]", "Rule at index " + i + " is null.");
        }

        var stateMachine = new StateMachine();
        var visited = new HashSet<State>();
        var stateToId = new Dictionary<State, string>();
        int stateCounter = 0;

        string initialId = $"S{stateCounter++}";
        stateMachine.AddState(initialId, initialState);
        stateMachine.StartingStateId = initialId;
        visited.Add(initialState);
        stateToId[initialState] = initialId;

        bool useDfs = config.ExplorationStrategy == ExplorationStrategy.DEPTHFIRSTSEARCH;
        var frontier = new LinkedList<(string id, State state, int depth)>();
        frontier.AddLast((initialId, initialState, 0));

        while (frontier.Count > 0)
        {
            var node = useDfs ? frontier.Last! : frontier.First!;
            var (currentId, currentState, currentDepth) = node.Value;
            frontier.Remove(node);

            if (config.MaxDepth.HasValue && currentDepth >= config.MaxDepth.Value)
                continue;

            foreach (var rule in rules)
            {
                if (rule.IsAvailable(currentState))
                {
                    var newState = rule.Execute(currentState);
                    string ruleName = rule.GetType().Name;

                    if (visited.Contains(newState))
                    {
                        string existingId = stateToId[newState];
                        stateMachine.Transitions.Add(new Transition(currentId, existingId, ruleName));
                    }
                    else
                    {
                        if (config.MaxStates.HasValue && stateMachine.States.Count >= config.MaxStates.Value)
                            break;

                        string newId = $"S{stateCounter++}";
                        stateMachine.AddState(newId, newState);
                        visited.Add(newState);
                        stateToId[newState] = newId;
                        stateMachine.Transitions.Add(new Transition(currentId, newId, ruleName));
                        frontier.AddLast((newId, newState, currentDepth + 1));
                    }
                }
            }
        }

        return stateMachine;
    }
}