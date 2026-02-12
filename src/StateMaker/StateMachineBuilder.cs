namespace StateMaker;

public class StateMachineBuilder : IStateMachineBuilder
{
    public StateMachine Build(State initialState, IRule[] rules, BuilderConfig config)
    {
        var stateMachine = new StateMachine();
        var visited = new HashSet<State>();
        var stateToId = new Dictionary<State, string>();
        var queue = new Queue<(string id, State state)>();
        int stateCounter = 0;

        string initialId = $"S{stateCounter++}";
        stateMachine.AddState(initialId, initialState);
        stateMachine.StartingStateId = initialId;
        visited.Add(initialState);
        stateToId[initialState] = initialId;
        queue.Enqueue((initialId, initialState));

        while (queue.Count > 0)
        {
            var (currentId, currentState) = queue.Dequeue();

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
                        string newId = $"S{stateCounter++}";
                        stateMachine.AddState(newId, newState);
                        visited.Add(newState);
                        stateToId[newState] = newId;
                        stateMachine.Transitions.Add(new Transition(currentId, newId, ruleName));
                        queue.Enqueue((newId, newState));
                    }
                }
            }
        }

        return stateMachine;
    }
}