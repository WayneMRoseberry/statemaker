namespace StateMaker.Tests;

public class FilterEngineTests
{
    private readonly IExpressionEvaluator _evaluator = new ExpressionEvaluator();

    #region Helper Methods

    private static StateMachine CreateMachineWithStates(params (string id, (string key, object? value)[] vars)[] states)
    {
        var machine = new StateMachine();
        foreach (var (id, vars) in states)
        {
            var state = new State();
            foreach (var (key, value) in vars)
            {
                state.Variables[key] = value;
            }
            machine.AddOrUpdateState(id, state);
        }
        if (states.Length > 0)
            machine.StartingStateId = states[0].id;
        return machine;
    }

    private static FilterDefinition CreateFilterDefinition(params (string condition, (string key, object? value)[] attrs)[] filters)
    {
        var definition = new FilterDefinition();
        foreach (var (condition, attrs) in filters)
        {
            var rule = new FilterRule { Condition = condition };
            foreach (var (key, value) in attrs)
            {
                rule.Attributes[key] = value;
            }
            definition.Filters.Add(rule);
        }
        return definition;
    }

    #endregion

    #region Single Rule Matching

    [Fact]
    public void Apply_SingleRuleMatchesOneState_ReturnsMatchingStateId()
    {
        var machine = CreateMachineWithStates(
            ("S0", new[] { ("Status", (object?)"Pending") }),
            ("S1", new[] { ("Status", (object?)"Approved") })
        );
        var filter = CreateFilterDefinition(
            ("[Status] == 'Approved'", new[] { ("ranking", (object?)"high") })
        );

        var result = new FilterEngine(_evaluator).Apply(machine, filter);

        Assert.Single(result.SelectedStateIds);
        Assert.Contains("S1", result.SelectedStateIds);
    }

    [Fact]
    public void Apply_SingleRuleMatchesMultipleStates_ReturnsAll()
    {
        var machine = CreateMachineWithStates(
            ("S0", new[] { ("Status", (object?)"Approved") }),
            ("S1", new[] { ("Status", (object?)"Approved") }),
            ("S2", new[] { ("Status", (object?)"Pending") })
        );
        var filter = CreateFilterDefinition(
            ("[Status] == 'Approved'", new[] { ("ranking", (object?)"high") })
        );

        var result = new FilterEngine(_evaluator).Apply(machine, filter);

        Assert.Equal(2, result.SelectedStateIds.Count);
        Assert.Contains("S0", result.SelectedStateIds);
        Assert.Contains("S1", result.SelectedStateIds);
    }

    #endregion

    #region No Matches

    [Fact]
    public void Apply_NoStatesMatch_ReturnsEmpty()
    {
        var machine = CreateMachineWithStates(
            ("S0", new[] { ("Status", (object?)"Pending") }),
            ("S1", new[] { ("Status", (object?)"Pending") })
        );
        var filter = CreateFilterDefinition(
            ("[Status] == 'Approved'", new[] { ("ranking", (object?)"high") })
        );

        var result = new FilterEngine(_evaluator).Apply(machine, filter);

        Assert.Empty(result.SelectedStateIds);
    }

    #endregion

    #region Attribute Assignment

    [Fact]
    public void Apply_MatchingRule_AppliesAttributesToState()
    {
        var machine = CreateMachineWithStates(
            ("S0", new[] { ("Status", (object?)"Approved") })
        );
        var filter = CreateFilterDefinition(
            ("[Status] == 'Approved'", new[] { ("ranking", (object?)"high"), ("priority", (object?)1) })
        );

        var result = new FilterEngine(_evaluator).Apply(machine, filter);

        var state = result.StateMachine.States["S0"];
        Assert.Equal("high", state.Attributes["ranking"]);
        Assert.Equal(1, state.Attributes["priority"]);
    }

    [Fact]
    public void Apply_NonMatchingRule_DoesNotApplyAttributes()
    {
        var machine = CreateMachineWithStates(
            ("S0", new[] { ("Status", (object?)"Pending") })
        );
        var filter = CreateFilterDefinition(
            ("[Status] == 'Approved'", new[] { ("ranking", (object?)"high") })
        );

        var result = new FilterEngine(_evaluator).Apply(machine, filter);

        var state = result.StateMachine.States["S0"];
        Assert.Empty(state.Attributes);
    }

    #endregion

    #region Multiple Rules with Attribute Merging

    [Fact]
    public void Apply_MultipleRulesMatch_MergesAttributes()
    {
        var machine = CreateMachineWithStates(
            ("S0", new[] { ("Status", (object?)"Approved"), ("Count", (object?)10) })
        );
        var filter = CreateFilterDefinition(
            ("[Status] == 'Approved'", new[] { ("ranking", (object?)"high") }),
            ("[Count] > 5", new[] { ("category", (object?)"large") })
        );

        var result = new FilterEngine(_evaluator).Apply(machine, filter);

        var state = result.StateMachine.States["S0"];
        Assert.Equal("high", state.Attributes["ranking"]);
        Assert.Equal("large", state.Attributes["category"]);
    }

    [Fact]
    public void Apply_MultipleRulesMatch_LaterRuleOverwritesDuplicateKeys()
    {
        var machine = CreateMachineWithStates(
            ("S0", new[] { ("Status", (object?)"Approved"), ("Count", (object?)10) })
        );
        var filter = CreateFilterDefinition(
            ("[Status] == 'Approved'", new[] { ("ranking", (object?)"high") }),
            ("[Count] > 5", new[] { ("ranking", (object?)"medium") })
        );

        var result = new FilterEngine(_evaluator).Apply(machine, filter);

        var state = result.StateMachine.States["S0"];
        Assert.Equal("medium", state.Attributes["ranking"]);
    }

    [Fact]
    public void Apply_OnlyFirstRuleMatches_OnlyFirstAttributesApplied()
    {
        var machine = CreateMachineWithStates(
            ("S0", new[] { ("Status", (object?)"Approved"), ("Count", (object?)2) })
        );
        var filter = CreateFilterDefinition(
            ("[Status] == 'Approved'", new[] { ("ranking", (object?)"high") }),
            ("[Count] > 5", new[] { ("category", (object?)"large") })
        );

        var result = new FilterEngine(_evaluator).Apply(machine, filter);

        var state = result.StateMachine.States["S0"];
        Assert.Equal("high", state.Attributes["ranking"]);
        Assert.False(state.Attributes.ContainsKey("category"));
    }

    #endregion

    #region State ID in Conditions

    [Fact]
    public void Apply_ConditionReferencesStateId_MatchesCorrectState()
    {
        var machine = CreateMachineWithStates(
            ("S0", new[] { ("Status", (object?)"Pending") }),
            ("S1", new[] { ("Status", (object?)"Pending") }),
            ("TargetState", new[] { ("Status", (object?)"Pending") })
        );
        var filter = CreateFilterDefinition(
            ($"[{FilterEngine.StateIdVariableName}] == 'TargetState'", new[] { ("selected", (object?)true) })
        );

        var result = new FilterEngine(_evaluator).Apply(machine, filter);

        Assert.Single(result.SelectedStateIds);
        Assert.Contains("TargetState", result.SelectedStateIds);
        Assert.Equal(true, result.StateMachine.States["TargetState"].Attributes["selected"]);
    }

    [Fact]
    public void Apply_ConditionCombinesStateIdAndVariable_MatchesCorrectly()
    {
        var machine = CreateMachineWithStates(
            ("S0", new[] { ("Status", (object?)"Approved") }),
            ("S1", new[] { ("Status", (object?)"Approved") }),
            ("S2", new[] { ("Status", (object?)"Pending") })
        );
        var filter = CreateFilterDefinition(
            ($"[{FilterEngine.StateIdVariableName}] == 'S1' && [Status] == 'Approved'", new[] { ("match", (object?)"exact") })
        );

        var result = new FilterEngine(_evaluator).Apply(machine, filter);

        Assert.Single(result.SelectedStateIds);
        Assert.Contains("S1", result.SelectedStateIds);
    }

    #endregion

    #region Expression Evaluation Errors

    [Fact]
    public void Apply_InvalidExpression_ThrowsExpressionEvaluationException()
    {
        var machine = CreateMachineWithStates(
            ("S0", new[] { ("Status", (object?)"Pending") })
        );
        var filter = CreateFilterDefinition(
            ("this is not valid !!!", new[] { ("ranking", (object?)"high") })
        );

        Assert.Throws<ExpressionEvaluationException>(() =>
            new FilterEngine(_evaluator).Apply(machine, filter));
    }

    #endregion

    #region Result StateMachine Preserves Structure

    [Fact]
    public void Apply_PreservesAllStatesInResultMachine()
    {
        var machine = CreateMachineWithStates(
            ("S0", new[] { ("Status", (object?)"Pending") }),
            ("S1", new[] { ("Status", (object?)"Approved") })
        );
        machine.Transitions.Add(new Transition("S0", "S1", "Approve"));

        var filter = CreateFilterDefinition(
            ("[Status] == 'Approved'", new[] { ("ranking", (object?)"high") })
        );

        var result = new FilterEngine(_evaluator).Apply(machine, filter);

        Assert.Equal(2, result.StateMachine.States.Count);
        Assert.Single(result.StateMachine.Transitions);
    }

    [Fact]
    public void Apply_EmptyFilterDefinition_ReturnsNoMatches()
    {
        var machine = CreateMachineWithStates(
            ("S0", new[] { ("Status", (object?)"Pending") })
        );
        var filter = new FilterDefinition();

        var result = new FilterEngine(_evaluator).Apply(machine, filter);

        Assert.Empty(result.SelectedStateIds);
    }

    #endregion
}
