using System.Xml.Serialization;

namespace OpenProtocol.Tool
{
    [XmlRoot("Curve File", Namespace = "http://www.atlascopco.com",
        IsNullable = false)]
    public class CurveFile : List<OpenProtocolTypes.TraceResult>
    {
    }
}