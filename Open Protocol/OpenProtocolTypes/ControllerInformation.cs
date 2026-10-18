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
            RawData ??= string.Empty;

            if (!short.TryParse(Slice(RawData, 22, 4), out _CellID))
            {
                _CellID = 0;
            }

            if (!short.TryParse(Slice(RawData, 28, 2), out _channelID))
            {
                _channelID = 0;
            }

            ControllerName = Slice(RawData, 32, 25).Trim();
            SupplierCode = Slice(RawData, 59, 3).Trim();
            OPVersion = Slice(RawData, 64, 19).Trim();
            ControllerVersion = Slice(RawData, 85, 19).Trim();
            ToolVersion = Slice(RawData, 106, 19).Trim();
            RBUType = Slice(RawData, 127, 24).Trim();
            ControllerSerial = Slice(RawData, 153, 10).Trim();
            SysType = ParseEnum(Slice(RawData, 165, 3), SystemTypeEnum.NotSet);
            SysSubType = ParseEnum(Slice(RawData, 170, 3), SystemSubTypeEnum.NotSet);
        }

        private static string Slice(string raw, int start, int length)
        {
            if (start < 0 || length <= 0 || raw.Length < start + length)
            {
                return string.Empty;
            }

            return raw.Substring(start, length);
        }

        private static TEnum ParseEnum<TEnum>(string text, TEnum fallback)
            where TEnum : struct, Enum
        {
            if (!int.TryParse(text, out var value) || !Enum.IsDefined(typeof(TEnum), value))
            {
                return fallback;
            }

            return (TEnum)Enum.ToObject(typeof(TEnum), value);
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