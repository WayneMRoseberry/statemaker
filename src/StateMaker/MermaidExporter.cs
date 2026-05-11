using System.Globalization;
using System.Text;

namespace StateMaker;

public class MermaidExporter : IStateMachineExporter
{
    public string Export(StateMachine stateMachine, ExportType exportType = ExportType.Default)
    {
        ArgumentNullException.ThrowIfNull(stateMachine);

        if (exportType != ExportType.Default)
            return ExportTraversals(stateMachine, exportType);

        var sb = new StringBuilder();
        sb.AppendLine("flowchart TD");

        // State definitions with labels
        foreach (var kvp in stateMachine.States)
        {
            var label = BuildStateLabel(kvp.Key, kvp.Value);
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"    {kvp.Key}[\"{label}\"]");
        }

        // Starting state indicator
        if (stateMachine.StartingStateId is not null)
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"    _start_((\" \")) --> {stateMachine.StartingStateId}");
            sb.AppendLine("    style _start_ fill:#000,stroke:#000,color:#000");
        }

        // Transitions
        foreach (var transition in stateMachine.Transitions)
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"    {transition.SourceStateId} -->|{transition.RuleName}| {transition.TargetStateId}");
        }

        return sb.ToString();
    }

    private static string ExportTraversals(StateMachine sm, ExportType exportType)
    {
        var traversals = TraversalGenerator.Generate(sm, exportType);
        var startId = sm.StartingStateId ?? string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("flowchart TD");

        foreach (var traversal in traversals)
        {
            var prefix = traversal.Id;
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"    subgraph {prefix}[\"{EscapeHtml(traversal.Name)}\"]");

            foreach (var stateId in GetTraversalStateIds(traversal, startId))
            {
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"        {prefix}_{stateId}[\"{EscapeHtml(stateId)}\"]");
            }

            foreach (var t in traversal.Transitions)
            {
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"        {prefix}_{t.SourceStateId} -->|{EscapeHtml(t.RuleName)}| {prefix}_{t.TargetStateId}");
            }

            sb.AppendLine("    end");
        }

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

    private static string BuildStateLabel(string stateId, State state)
    {
        var parts = new List<string>();
        parts.Add(EscapeHtml(stateId));
        foreach (var kvp in state.Variables)
        {
            parts.Add($"{EscapeHtml(kvp.Key)}={FormatValue(kvp.Value)}");
        }
        if (state.Attributes.Count > 0)
        {
            parts.Add("---");
            foreach (var kvp in state.Attributes)
            {
                parts.Add($"{EscapeHtml(kvp.Key)}={FormatValue(kvp.Value)}");
            }
        }
        return string.Join("<br />", parts);
    }

    private static string FormatValue(object? value)
    {
        var raw = value switch
        {
            string s => $"'{s}'",
            bool b => b ? "true" : "false",
            null => "null",
            _ => string.Format(CultureInfo.InvariantCulture, "{0}", value)
        };
        return EscapeHtml(raw);
    }

    private static string EscapeHtml(string text)
    {
        return text.Replace("&", "&amp;", StringComparison.Ordinal)
                   .Replace("<", "&lt;", StringComparison.Ordinal)
                   .Replace(">", "&gt;", StringComparison.Ordinal)
                   .Replace("\"", "&quot;", StringComparison.Ordinal);
    }
}
