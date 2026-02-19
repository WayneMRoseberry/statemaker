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
                $"    \"{EscapeDot(kvp.Key)}\" [label=\"{EscapeDot(label)}\"];");
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
        var sb = new StringBuilder();
        sb.Append(stateId);
        if (state.Variables.Count > 0)
        {
            sb.Append("\\n");
            foreach (var kvp in state.Variables)
            {
                sb.Append(CultureInfo.InvariantCulture, $"{kvp.Key}={FormatValue(kvp.Value)}\\n");
            }
        }
        return sb.ToString().TrimEnd('\\', 'n');
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
