using System.Xml.Serialization;

namespace OpenProtocol.Tool
{
    [XmlRoot("Result File", Namespace = "http://www.atlascopco.com",
        IsNullable = false)]
    [Serializable]
    public class ResultFile
    {
        public OpenProtocolTypes.TightResultStation? StationTightResult { get; set; }
        public OpenProtocolTypes.PressResultStation? StationPressResult { get; set; }
        public List<OpenProtocolTypes.ResultBolt> BoltTightResults { get; set; } = new();
        public List<OpenProtocolTypes.ResultFitting> BoltFittingResults { get; set; } = new();


        public ResultFile()
        {
        }

        public ResultFile(OpenProtocolTypes.SystemSubTypeEnum SubType)
        {
            switch (SubType)
            {
                case OpenProtocolTypes.SystemSubTypeEnum.Normal:
                    StationTightResult = new OpenProtocolTypes.TightResultStation();
                    BoltTightResults = new List<OpenProtocolTypes.ResultBolt>();

                    break;
                case OpenProtocolTypes.SystemSubTypeEnum.Press:
                    StationPressResult = new OpenProtocolTypes.PressResultStation();
                    BoltFittingResults = new List<OpenProtocolTypes.ResultFitting>();
                    break;
                default:
                    break;
            }
        }
    }
}