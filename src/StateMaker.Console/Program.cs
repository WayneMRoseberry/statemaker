using StateMaker;

namespace StateMaker.Console;

public class Program
{
    public static int Main(string[] args)
    {
        return Run(args, System.Console.Out, System.Console.Error);
    }

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length == 0)
        {
            HelpPrinter.PrintHelp(stdout);
            return 0;
        }

        var command = args[0].ToLowerInvariant();

        try
        {
            switch (command)
            {
                case "build":
                    return RunBuild(args, stdout, stderr);
                case "export":
                    return RunExport(args, stdout, stderr);
                case "filter":
                    return RunFilter(args, stdout, stderr);
                default:
                    stderr.WriteLine($"Unknown command '{args[0]}'.");
                    stderr.WriteLine();
                    HelpPrinter.PrintHelp(stderr);
                    return 1;
            }
        }
        catch (Exception ex)
        {
            stderr.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static int RunBuild(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length < 2)
        {
            stderr.WriteLine("Error: build command requires a definition file path.");
            return 1;
        }

        var filePath = args[1];
        var format = GetOptionValue(args, "--format", "-f") ?? "json";
        var outputPath = GetOptionValue(args, "--output", "-o");

        var buildCommand = new BuildCommand();
        buildCommand.Execute(filePath, outputPath, format, stdout);
        return 0;
    }

    private static int RunExport(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length < 2)
        {
            stderr.WriteLine("Error: export command requires a state machine file path.");
            return 1;
        }

        var filePath = args[1];
        var format = GetOptionValue(args, "--format", "-f") ?? "json";
        var outputPath = GetOptionValue(args, "--output", "-o");

        var filterPath = GetOptionValue(args, "--filter", "--filter");

        var exportCommand = new ExportCommand();
        exportCommand.Execute(filePath, outputPath, format, stdout, filterPath);
        return 0;
    }

    private static int RunFilter(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length < 3)
        {
            stderr.WriteLine("Error: filter command requires a state machine file path and a filter definition file path.");
            return 1;
        }

        var smFilePath = args[1];
        var filterFilePath = args[2];
        var format = GetOptionValue(args, "--format", "-f") ?? "json";
        var outputPath = GetOptionValue(args, "--output", "-o");
        var list = HasFlag(args, "--list");

        var filterCommand = new FilterCommand();
        filterCommand.Execute(smFilePath, filterFilePath, outputPath, format, stdout, list: list);
        return 0;
    }

    private static bool HasFlag(string[] args, string flag)
    {
        return args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetOptionValue(string[] args, string longFlag, string shortFlag)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], longFlag, StringComparison.OrdinalIgnoreCase)
                || string.Equals(args[i], shortFlag, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
