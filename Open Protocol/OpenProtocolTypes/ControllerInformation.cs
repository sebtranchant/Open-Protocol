namespace OpenProtocol.OpenProtocolTypes
{
    public class ControllerInformation
    {
        private short _CellID;
        private short _channelID;

        public short CellID
        {
            get { return _CellID; }
        }

        public short ChannelID
        {
            get => _channelID;
            private set => _channelID = value;
        }

        public string ControllerName { get; private set; }
        public string SupplierCode { get; private set; }
        public string OPVersion { get; private set; }
        public string ControllerVersion { get; private set; }
        public string ToolVersion { get; private set; }
        public string RBUType { get; private set; }
        public string ControllerSerial { get; private set; }
        public SystemTypeEnum SysType { get; private set; }
        public SystemSubTypeEnum SysSubType { get; private set; }

        public ControllerInformation(string RawData)
        {
            if (!short.TryParse(RawData.Substring(22, 4), out _CellID))
            {
                _CellID = 0;
            }

            if (!short.TryParse(RawData.Substring(28, 2), out _channelID))
            {
                _channelID = 0;
            }


            ControllerName = RawData.Substring(32, 25).Trim();
            SupplierCode = RawData.Substring(59, 3).Trim();
            OPVersion = RawData.Length >= 64 + 19 ? RawData.Substring(64, 19).Trim() : OPVersion = "";
            ControllerVersion = RawData.Length > 85 + 19 ? RawData.Substring(85, 19).Trim() : ControllerVersion = "";
            ToolVersion = RawData.Length >= 106 + 19 ? RawData.Substring(106, 19).Trim() : ToolVersion = "";
            RBUType = RawData.Length >= 127 + 24 ? RawData.Substring(127, 24).Trim() : RBUType = "";
            ControllerSerial = RawData.Length >= 153 + 10 ? RawData.Substring(153, 10).Trim() : ControllerSerial = "";
            SysType = RawData.Length >= 165 + 3
                ? (SystemTypeEnum)int.Parse(RawData.Substring(165, 3))
                : SysType = SystemTypeEnum.NotSet;
            SysSubType = RawData.Length >= 170 + 3
                ? (SystemSubTypeEnum)int.Parse(RawData.Substring(170, 3))
                : SysSubType = SystemSubTypeEnum.NotSet;
        }
    }

    public enum SystemTypeEnum
    {
        NotSet,
        PF4000,
        PM4000,
        PF6000,
        MT6000
    }

    public enum SystemSubTypeEnum
    {
        Normal = 1,
        Press = 2,
        NotSet = 3
    }
}