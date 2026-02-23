using System.Globalization;
using System.Text;

namespace StateMaker;

public class DotExporter : IStateMachineExporter
{
    public string Export(StateMachine stateMachine)
    {
        ArgumentNullException.ThrowIfNull(stateMachine);

        var sb = new StringBuilder();
        sb.AppendLine("digraph StateMachine {");
        sb.AppendLine("    rankdir=LR;");
        sb.AppendLine("    node [shape=box];");

        // Starting state indicator
        if (stateMachine.StartingStateId is not null)
        {
            sb.AppendLine("    __start [shape=point, width=0.2, label=\"\"];");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"    __start -> \"{EscapeDot(stateMachine.StartingStateId)}\";");
        }

        // Nodes
        foreach (var kvp in stateMachine.States)
        {
            var label = BuildNodeLabel(kvp.Key, kvp.Value);
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"    \"{EscapeDot(kvp.Key)}\" [label=\"{label}\"];");
        }

        // Edges
        foreach (var transition in stateMachine.Transitions)
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"    \"{EscapeDot(transition.SourceStateId)}\" -> \"{EscapeDot(transition.TargetStateId)}\" [label=\"{EscapeDot(transition.RuleName)}\"];");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string BuildNodeLabel(string stateId, State state)
    {
        var parts = new List<string>();
        parts.Add(EscapeDot(stateId));
        foreach (var kvp in state.Variables)
        {
            parts.Add($"{EscapeDot(kvp.Key)}={EscapeDot(FormatValue(kvp.Value))}");
        }
        if (state.Attributes.Count > 0)
        {
            parts.Add("---");
            foreach (var kvp in state.Attributes)
            {
                parts.Add($"{EscapeDot(kvp.Key)}={EscapeDot(FormatValue(kvp.Value))}");
            }
        }
        return string.Join("\\n", parts);
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            string s => $"'{s}'",
            bool b => b ? "true" : "false",
            null => "null",
            _ => string.Format(CultureInfo.InvariantCulture, "{0}", value)
        };
    }

    private static string EscapeDot(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
                     .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
