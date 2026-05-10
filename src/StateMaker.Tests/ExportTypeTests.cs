using Xunit;

namespace StateMaker.Tests;

public class ExportTypeTests
{
    [Fact]
    public void Parse_NullOrEmpty_ReturnsDefault()
    {
        Assert.Equal(ExportType.Default, ExportTypeParser.Parse(null));
        Assert.Equal(ExportType.Default, ExportTypeParser.Parse(string.Empty));
        Assert.Equal(ExportType.Default, ExportTypeParser.Parse("   "));
    }

    [Theory]
    [InlineData("Default", ExportType.Default)]
    [InlineData("allstates", ExportType.AllStates)]
    [InlineData("AllTransitions", ExportType.AllTransitions)]
    [InlineData("ALLPATHS", ExportType.AllPaths)]
    [InlineData("AllStatePairs", ExportType.AllStatePairs)]
    public void Parse_ValidValue_ReturnsExpectedExportType(string value, ExportType expected)
    {
        Assert.Equal(expected, ExportTypeParser.Parse(value));
    }

    [Fact]
    public void Parse_InvalidValue_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => ExportTypeParser.Parse("unsupported"));
        Assert.Contains("Unsupported export type", ex.Message, StringComparison.Ordinal);
    }
}
