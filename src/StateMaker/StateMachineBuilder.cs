namespace StateMaker;

public class StateMachineBuilder : IStateMachineBuilder
{
    private readonly IStateMachineLogger? _logger;

    public StateMachineBuilder()
    {
    }

    public StateMachineBuilder(IStateMachineLogger logger)
    {
        _logger = logger;
    }

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
        var stateToId = new Dictionary<State, string>();
        int stateCounter = 0;

        string initialId = $"S{stateCounter++}";
        stateMachine.AddOrUpdateState(initialId, initialState);
        stateMachine.StartingStateId = initialId;
        stateToId[initialState] = initialId;

        Log(config, LogLevel.INFO, $"Initial state added as {initialId}");

        // TODO: this is only going to work so long as there are only two exploration strategies.
        // If more strategies are added in the future, this logic will need to be refactored to accommodate them.
        bool useDfs = config.ExplorationStrategy == ExplorationStrategy.DEPTHFIRSTSEARCH;
        var frontier = new LinkedList<(string id, State state, int depth)>();
        frontier.AddLast((initialId, initialState, 0));

        Log(config, LogLevel.INFO, $"Starting exploration using {config.ExplorationStrategy}");

        while (frontier.Count > 0)
        {
            var node = useDfs ? frontier.Last! : frontier.First!;
            var (currentId, currentState, currentDepth) = node.Value;
            frontier.Remove(node);

            if (config.MaxDepth.HasValue && currentDepth >= config.MaxDepth.Value)
            {
                Log(config, LogLevel.DEBUG, $"Max depth {config.MaxDepth.Value} reached at {currentId}");
                continue;
            }

            foreach (var rule in rules)
            {
                if (rule.IsAvailable(currentState))
                {
                    var newState = rule.Execute(currentState);
                    string ruleName = rule.GetName();

                    Log(config, LogLevel.DEBUG, $"Rule '{ruleName}' applied to {currentId}");

                    // TODO: see what happens when two rules return the same name to the same state from the same
                    // source state. This will result in duplicate transitions with the same source, target, and name.
                    // Explore this condition and determine whether it is desirable to allow duplicate transitions or if additional logic is needed to prevent them.
                    if (stateToId.TryGetValue(newState, out string? existingId))
                    {
                        stateMachine.Transitions.Add(new Transition(currentId, existingId, ruleName));
                        Log(config, LogLevel.DEBUG, $"Cycle detected: {currentId} -> {existingId} via '{ruleName}'");
                    }
                    // TODO: note for testing that this create a condition where order of rules can affect the
                    // state machine structure. If the we are at max states count, the first rule that generates
                    // a new state will be added, while subsequent rules that generate new states will be ignored.
                    // This can lead to different state machine structures based on the order of rules, which may
                    // have implications for testing and reproducibility. Consider whether additional logic is needed
                    // to handle this condition, such as prioritizing certain rules or implementing a tie-breaking
                    // mechanism when multiple rules generate new states at the same depth level.
                    else
                    {
                        if (config.MaxStates.HasValue && stateMachine.States.Count >= config.MaxStates.Value)
                        {
                            Log(config, LogLevel.INFO, $"Max states limit {config.MaxStates.Value} reached");
                            break;
                        }

                        string newId = $"S{stateCounter++}";
                        stateMachine.AddOrUpdateState(newId, newState);
                        stateToId[newState] = newId;
                        stateMachine.Transitions.Add(new Transition(currentId, newId, ruleName));
                        frontier.AddLast((newId, newState, currentDepth + 1));

                        Log(config, LogLevel.INFO, $"New state {newId} discovered via '{ruleName}' from {currentId}");
                    }
                }
            }
        }

        Log(config, LogLevel.INFO, $"Exploration complete: {stateMachine.States.Count} states, {stateMachine.Transitions.Count} transitions");

        return stateMachine;
    }

    private void Log(BuilderConfig config, LogLevel level, string message)
    {
        if (_logger is null)
            return;

        switch (level)
        {
            case LogLevel.INFO:
                _logger.LogInfo(message);
                break;
            case LogLevel.DEBUG:
                _logger.LogDebug(message);
                break;
            case LogLevel.ERROR:
                _logger.LogError(message);
                break;
        }
    }
}
