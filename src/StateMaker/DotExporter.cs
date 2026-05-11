using System.Globalization;
using System.Text;

namespace StateMaker;

public class DotExporter : IStateMachineExporter
{
    public string Export(StateMachine stateMachine, ExportType exportType = ExportType.Default)
    {
        ArgumentNullException.ThrowIfNull(stateMachine);

        if (exportType != ExportType.Default)
            return ExportTraversals(stateMachine, exportType);

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

    private static string ExportTraversals(StateMachine sm, ExportType exportType)
    {
        var traversals = TraversalGenerator.Generate(sm, exportType);
        var startId = sm.StartingStateId ?? string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("digraph TraversalCoverage {");
        sb.AppendLine("    rankdir=LR;");
        sb.AppendLine("    node [shape=box];");

        foreach (var traversal in traversals)
        {
            var clusterId = EscapeDot(traversal.Id);
            sb.AppendLine(CultureInfo.InvariantCulture, $"    subgraph cluster_{clusterId} {{");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        label=\"{EscapeDot(traversal.Name)}\";");

            foreach (var stateId in GetTraversalStateIds(traversal, startId))
            {
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"        \"{clusterId}_{EscapeDot(stateId)}\" [label=\"{EscapeDot(stateId)}\"];");
            }

            foreach (var t in traversal.Transitions)
            {
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"        \"{clusterId}_{EscapeDot(t.SourceStateId)}\" -> \"{clusterId}_{EscapeDot(t.TargetStateId)}\" [label=\"{EscapeDot(t.RuleName)}\"];");
            }

            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static IEnumerable<string> GetTraversalStateIds(Traversal traversal, string startStateId)
    {
        if (traversal.Transitions.Count == 0)
            return new[] { startStateId };

        var seen = new HashSet<string>();
        var result = new List<string>();
        foreach (var t in traversal.Transitions)
        {
            if (seen.Add(t.SourceStateId)) result.Add(t.SourceStateId);
            if (seen.Add(t.TargetStateId)) result.Add(t.TargetStateId);
        }
        return result;
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
