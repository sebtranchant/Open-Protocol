using System.Xml.Serialization;

namespace OpenProtocol.Tool

{
    [XmlRoot("Plot File", Namespace = "http://www.atlascopco.com",
        IsNullable = false)]
    public class PlotFile : List<OpenProtocolTypes.TracePlot>
    {
    }
}