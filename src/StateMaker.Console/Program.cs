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

        var exportCommand = new ExportCommand();
        exportCommand.Execute(filePath, outputPath, format, stdout);
        return 0;
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
