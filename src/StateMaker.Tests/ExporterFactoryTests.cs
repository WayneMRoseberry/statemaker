using Xunit;

namespace StateMaker.Tests;

public class ExporterFactoryTests
{
    [Fact]
    public void GetExporter_Json_ReturnsJsonExporter()
    {
        var exporter = ExporterFactory.GetExporter("json");

        Assert.IsType<JsonExporter>(exporter);
    }

    [Fact]
    public void GetExporter_Dot_ReturnsDotExporter()
    {
        var exporter = ExporterFactory.GetExporter("dot");

        Assert.IsType<DotExporter>(exporter);
    }

    [Fact]
    public void GetExporter_Graphml_ReturnsGraphMlExporter()
    {
        var exporter = ExporterFactory.GetExporter("graphml");

        Assert.IsType<GraphMlExporter>(exporter);
    }

    [Fact]
    public void GetExporter_Mermaid_ReturnsMermaidExporter()
    {
        var exporter = ExporterFactory.GetExporter("mermaid");

        Assert.IsType<MermaidExporter>(exporter);
    }

    [Fact]
    public void GetExporter_CaseInsensitive_Mermaid()
    {
        var exporter = ExporterFactory.GetExporter("MERMAID");

        Assert.IsType<MermaidExporter>(exporter);
    }

    [Fact]
    public void GetExporter_CaseInsensitive_Json()
    {
        var exporter = ExporterFactory.GetExporter("JSON");

        Assert.IsType<JsonExporter>(exporter);
    }

    [Fact]
    public void GetExporter_CaseInsensitive_Dot()
    {
        var exporter = ExporterFactory.GetExporter("DOT");

        Assert.IsType<DotExporter>(exporter);
    }

    [Fact]
    public void GetExporter_CaseInsensitive_GraphMl()
    {
        var exporter = ExporterFactory.GetExporter("GraphML");

        Assert.IsType<GraphMlExporter>(exporter);
    }

    [Fact]
    public void GetExporter_UnknownFormat_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ExporterFactory.GetExporter("xml"));

        Assert.Contains("xml", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetExporter_NullFormat_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExporterFactory.GetExporter(null!));
    }
}
