using System.Xml.Serialization;

namespace OpenProtocol.OpenProtocolTypes
{
    [XmlRoot("Trace plot", Namespace = "http://www.atlascopco.com",
        IsNullable = false)]
    public class TracePlot
    {
        public uint ResultDataIdentifier { get; set; }
        public DateTime TimeStamp { get; set; }
        public short NumberOfPIDs { get; set; }
        public List<DataField> Datafields { get; set; } = new();

        public TracePlot()
        {
        }

        public TracePlot(string Message)
        {
            string MessWithoutHeader = Message.Substring(20, Message.Length - 21);

            this.ResultDataIdentifier = uint.Parse(MessWithoutHeader.Substring(0, 10));
            try
            {
                this.TimeStamp = DateTime.ParseExact(MessWithoutHeader.Substring(10, 19), "yyyy-MM-dd:HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw;
            }

            this.NumberOfPIDs = short.Parse(MessWithoutHeader.Substring(29, 3));

            #region Datafields

            int PIDLength = 0;
            if (NumberOfPIDs > 0)
            {
                Datafields = new List<DataField>();
                int Datalength = 0;
                int offset = 32; // Offset du premier datafield
                for (short i = 0; i < NumberOfPIDs; i++)
                {
                    Datalength = short.Parse(MessWithoutHeader.Substring(offset + 5, 3));

                    Datafields.Add(new DataField(MessWithoutHeader.Substring(offset, 17 + Datalength)));
                    offset = offset + (Datalength + 17);
                }

                PIDLength = offset;
            }

            #endregion
        }
    }
}