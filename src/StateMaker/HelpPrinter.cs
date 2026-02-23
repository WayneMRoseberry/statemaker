namespace StateMaker;

public static class HelpPrinter
{
    public static void PrintHelp(TextWriter writer)
    {
        writer.WriteLine("Usage: statemaker <command> [options]");
        writer.WriteLine();
        writer.WriteLine("Commands:");
        writer.WriteLine("  build <file>               Build a state machine from a definition file");
        writer.WriteLine("  export <file>              Load a JSON state machine and export to another format");
        writer.WriteLine("  filter <file> <filter>     Apply a filter to a state machine and export the result");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --format, -f <format>   Export format: json, dot, graphml, mermaid (default: json)");
        writer.WriteLine("  --output, -o <file>     Output file path (default: stdout)");
        writer.WriteLine("  --filter <file>         Filter definition file (for export command)");
        writer.WriteLine();
        writer.WriteLine("Examples:");
        writer.WriteLine("  statemaker build definition.json");
        writer.WriteLine("  statemaker build definition.json --format dot --output graph.dot");
        writer.WriteLine("  statemaker export machine.json --format graphml -o machine.graphml");
        writer.WriteLine("  statemaker export machine.json --filter filter.json --format dot");
        writer.WriteLine("  statemaker filter machine.json filter.json --format dot");
    }
}
