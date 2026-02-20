using Xunit;

namespace StateMaker.Tests;

public class HelpPrinterTests
{
    [Fact]
    public void PrintHelp_ContainsUsageSyntax()
    {
        var writer = new StringWriter();

        HelpPrinter.PrintHelp(writer);

        var output = writer.ToString();
        Assert.Contains("Usage:", output, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintHelp_ContainsBuildCommand()
    {
        var writer = new StringWriter();

        HelpPrinter.PrintHelp(writer);

        var output = writer.ToString();
        Assert.Contains("build", output, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintHelp_ContainsExportCommand()
    {
        var writer = new StringWriter();

        HelpPrinter.PrintHelp(writer);

        var output = writer.ToString();
        Assert.Contains("export", output, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintHelp_ContainsFormatOption()
    {
        var writer = new StringWriter();

        HelpPrinter.PrintHelp(writer);

        var output = writer.ToString();
        Assert.Contains("--format", output, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintHelp_ContainsOutputOption()
    {
        var writer = new StringWriter();

        HelpPrinter.PrintHelp(writer);

        var output = writer.ToString();
        Assert.Contains("--output", output, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintHelp_ContainsExamples()
    {
        var writer = new StringWriter();

        HelpPrinter.PrintHelp(writer);

        var output = writer.ToString();
        Assert.Contains("Examples:", output, StringComparison.Ordinal);
    }
}
