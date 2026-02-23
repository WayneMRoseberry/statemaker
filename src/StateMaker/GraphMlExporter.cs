using System.Globalization;
using System.Text;
using System.Xml;

namespace StateMaker;

public class GraphMlExporter : IStateMachineExporter
{
    public string Export(StateMachine stateMachine)
    {
        ArgumentNullException.ThrowIfNull(stateMachine);

        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        });

        writer.WriteStartDocument();
        writer.WriteStartElement("graphml", "http://graphml.graphstruct.org/xmlns");
        writer.WriteAttributeString("xmlns", "y", null, "http://www.yworks.com/xml/graphml");

        // Key definitions for yEd
        writer.WriteStartElement("key");
        writer.WriteAttributeString("id", "d0");
        writer.WriteAttributeString("for", "node");
        writer.WriteAttributeString("yfiles.type", "nodegraphics");
        writer.WriteEndElement();

        writer.WriteStartElement("key");
        writer.WriteAttributeString("id", "d1");
        writer.WriteAttributeString("for", "edge");
        writer.WriteAttributeString("yfiles.type", "edgegraphics");
        writer.WriteEndElement();

        writer.WriteStartElement("graph");
        writer.WriteAttributeString("id", "G");
        writer.WriteAttributeString("edgedefault", "directed");

        // Nodes
        foreach (var kvp in stateMachine.States)
        {
            var isStarting = kvp.Key == stateMachine.StartingStateId;
            WriteNode(writer, kvp.Key, kvp.Value, isStarting);
        }

        // Edges
        int edgeId = 0;
        foreach (var transition in stateMachine.Transitions)
        {
            WriteEdge(writer, $"e{edgeId++}", transition);
        }

        writer.WriteEndElement(); // graph
        writer.WriteEndElement(); // graphml
        writer.WriteEndDocument();
        writer.Flush();

        return sb.ToString();
    }

    private static void WriteNode(XmlWriter writer, string stateId, State state, bool isStarting)
    {
        writer.WriteStartElement("node");
        writer.WriteAttributeString("id", stateId);

        writer.WriteStartElement("data");
        writer.WriteAttributeString("key", "d0");

        writer.WriteStartElement("ShapeNode", "http://www.yworks.com/xml/graphml");

        // Geometry
        writer.WriteStartElement("Geometry", "http://www.yworks.com/xml/graphml");
        writer.WriteAttributeString("height", "60.0");
        writer.WriteAttributeString("width", "120.0");
        writer.WriteEndElement();

        // Fill color
        writer.WriteStartElement("Fill", "http://www.yworks.com/xml/graphml");
        writer.WriteAttributeString("color", isStarting ? "#CCFFCC" : "#FFFFFF");
        writer.WriteEndElement();

        // Border
        writer.WriteStartElement("BorderStyle", "http://www.yworks.com/xml/graphml");
        writer.WriteAttributeString("color", "#000000");
        writer.WriteAttributeString("type", "line");
        writer.WriteAttributeString("width", isStarting ? "2.0" : "1.0");
        writer.WriteEndElement();

        // Label
        writer.WriteStartElement("NodeLabel", "http://www.yworks.com/xml/graphml");
        writer.WriteString(BuildNodeLabel(stateId, state));
        writer.WriteEndElement();

        // Shape
        writer.WriteStartElement("Shape", "http://www.yworks.com/xml/graphml");
        writer.WriteAttributeString("type", "roundrectangle");
        writer.WriteEndElement();

        writer.WriteEndElement(); // ShapeNode
        writer.WriteEndElement(); // data
        writer.WriteEndElement(); // node
    }

    private static void WriteEdge(XmlWriter writer, string edgeId, Transition transition)
    {
        writer.WriteStartElement("edge");
        writer.WriteAttributeString("id", edgeId);
        writer.WriteAttributeString("source", transition.SourceStateId);
        writer.WriteAttributeString("target", transition.TargetStateId);

        writer.WriteStartElement("data");
        writer.WriteAttributeString("key", "d1");

        writer.WriteStartElement("PolyLineEdge", "http://www.yworks.com/xml/graphml");

        writer.WriteStartElement("LineStyle", "http://www.yworks.com/xml/graphml");
        writer.WriteAttributeString("color", "#000000");
        writer.WriteAttributeString("type", "line");
        writer.WriteAttributeString("width", "1.0");
        writer.WriteEndElement();

        writer.WriteStartElement("Arrows", "http://www.yworks.com/xml/graphml");
        writer.WriteAttributeString("source", "none");
        writer.WriteAttributeString("target", "standard");
        writer.WriteEndElement();

        writer.WriteStartElement("EdgeLabel", "http://www.yworks.com/xml/graphml");
        writer.WriteString(transition.RuleName);
        writer.WriteEndElement();

        writer.WriteEndElement(); // PolyLineEdge
        writer.WriteEndElement(); // data
        writer.WriteEndElement(); // edge
    }

    private static string BuildNodeLabel(string stateId, State state)
    {
        var sb = new StringBuilder();
        sb.Append(stateId);
        foreach (var kvp in state.Variables)
        {
            sb.Append(CultureInfo.InvariantCulture, $"\n{kvp.Key}={FormatValue(kvp.Value)}");
        }
        if (state.Attributes.Count > 0)
        {
            sb.Append("\n---");
            foreach (var kvp in state.Attributes)
            {
                sb.Append(CultureInfo.InvariantCulture, $"\n{kvp.Key}={FormatValue(kvp.Value)}");
            }
        }
        return sb.ToString();
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
}
