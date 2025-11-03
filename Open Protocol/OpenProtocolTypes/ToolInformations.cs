using System.Globalization;

namespace OpenProtocol.OpenProtocolTypes
{
    public class ToolInformations
    {
        public string ToolSerialNumber { get; private set; }
        public string NumberOfTightenings { get; private set; }

        public string Calibrationvalue { get; private set; }
        public System.DateTime CalibrationDate { get; private set; }

        public string toolSpeed { get; private set; }

        public ToolInformations(string RawData)
        {
            ToolSerialNumber = RawData.Substring(22, 14).Trim();
            NumberOfTightenings = RawData.Substring(38, 10).Trim();
            Calibrationvalue = RawData.Substring(83, 6).Trim();
            if(RawData.Length > 179)
            {
                toolSpeed = RawData.Substring(174, 6).Trim();
            }
            else
            {
                toolSpeed = "0";
            }
               


            CultureInfo provider = CultureInfo.InvariantCulture;
            var value = RawData.Substring(50, 19);
            string format = "yyyy-MM-dd:hh:mm:ss";
            try
            {
                CalibrationDate = DateTime.ParseExact(value, format, provider);
            }
            catch (Exception)
            {
                CalibrationDate = new DateTime(1900, 1, 1);
            }
        }
    }
}