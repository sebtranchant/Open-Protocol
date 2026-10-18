using System.Globalization;

namespace OpenProtocol.OpenProtocolTypes
{
    public class ResultEvtArgs : EventArgs
    {
        private const int MinimumLength = 313;

        public string Mid { get; }
        public string data { get; }
        public int TightId { get; }
        public DateTime? Date { get; }
        public float Couple { get; }
        public float CoupleMin { get; }
        public float CoupleCible { get; }
        public float CoupleMax { get; }
        public float PrevailTorque { get; }
        public ResultPFValueSatus CoupleStatus { get; }
        public int Angle { get; }
        public int AngleMin { get; }
        public int AngleCible { get; }
        public int AngleMax { get; }
        public ResultPFValueSatus AngleStatus { get; }
        public string Status { get; }
        public int Bacthsize { get; }
        public int Counter { get; }
        public string BatchStatus { get; }
        public string TCName { get; }
        public int ChannelID { get; }
        public int JobID { get; }
        public string ToolSerial { get; }
        public string PsetName { get; }
        public string Indentifier1 { get; }
        public string Indentifier2 { get; }
        public string Indentifier3 { get; }
        public ResultType RsltType { get; }

        public ResultEvtArgs(string ResultMessage)
        {
            if (string.IsNullOrEmpty(ResultMessage) || ResultMessage.Length < MinimumLength)
            {
                throw new FormatException("Result message is too short.");
            }

            Mid = ResultMessage.Substring(4, 4);
            data = ResultMessage.Substring(20);
            TightId = ReadInt(ResultMessage, 304, 9);
            Date = ParseDate(Slice(ResultMessage, 345, 19));
            Couple = ReadTorque(ResultMessage, 183);
            CoupleMin = ReadTorque(ResultMessage, 159);
            CoupleCible = ReadTorque(ResultMessage, 175);
            CoupleMax = ReadTorque(ResultMessage, 167);
            PrevailTorque = ReadOptionalTorque(ResultMessage, 295);
            CoupleStatus = ReadEnum<ResultPFValueSatus>(ResultMessage, 126, 1);
            Angle = ReadInt(ResultMessage, 212, 5);
            AngleMin = ReadInt(ResultMessage, 191, 5);
            AngleCible = ReadInt(ResultMessage, 205, 5);
            AngleMax = ReadInt(ResultMessage, 198, 5);
            AngleStatus = ReadEnum<ResultPFValueSatus>(ResultMessage, 129, 1);
            Status = ReadOkNok(ResultMessage, 120);
            Bacthsize = ReadInt(ResultMessage, 109, 3);
            Counter = ReadInt(ResultMessage, 115, 3);
            BatchStatus = ReadOkNok(ResultMessage, 219);
            TCName = ResultMessage.Substring(32, 25).Trim();
            ChannelID = ReadInt(ResultMessage, 28, 2);
            JobID = ReadInt(ResultMessage, 86, 4);
            ToolSerial = Slice(ResultMessage, 329, 14).Trim();
            PsetName = Slice(ResultMessage, 387, 25).Trim();
            Indentifier1 = ResultMessage.Substring(59, 25).Trim();
            Indentifier2 = Slice(ResultMessage, 421, 25).Trim();
            Indentifier3 = Slice(ResultMessage, 448, 25).Trim();
            RsltType = ReadOptionalEnum<ResultType>(ResultMessage, 417, 2);
        }

        private static string Slice(string message, int start, int length)
        {
            if (message.Length < start + length)
            {
                return string.Empty;
            }

            return message.Substring(start, length);
        }

        private static int ReadInt(string message, int start, int length)
        {
            if (!int.TryParse(message.AsSpan(start, length), NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                throw new FormatException("Result field is not numeric.");
            }

            return value;
        }

        private static float ReadTorque(string message, int start)
        {
            if (!float.TryParse(message.AsSpan(start, 6), NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                throw new FormatException("Result torque is not numeric.");
            }

            return value / 100f;
        }

        private static float ReadOptionalTorque(string message, int start)
        {
            var slice = Slice(message, start, 6);
            if (!float.TryParse(slice, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                return 0f;
            }

            return value / 100f;
        }

        private static TEnum ReadEnum<TEnum>(string message, int start, int length)
            where TEnum : struct, Enum
        {
            if (!int.TryParse(message.AsSpan(start, length), NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                throw new FormatException("Result status is not numeric.");
            }

            return (TEnum)Enum.ToObject(typeof(TEnum), value);
        }

        private static TEnum ReadOptionalEnum<TEnum>(string message, int start, int length)
            where TEnum : struct, Enum
        {
            var slice = Slice(message, start, length);
            if (!int.TryParse(slice, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                return default;
            }

            return (TEnum)Enum.ToObject(typeof(TEnum), value);
        }

        private static string ReadOkNok(string message, int start)
        {
            return message[start] switch
            {
                '0' => "NOK",
                '1' => "OK",
                _ => "N/A"
            };
        }

        private static DateTime? ParseDate(string value)
        {
            if (value.Length != 19)
            {
                return null;
            }

            if (!DateTime.TryParseExact(value, "yyyy-MM-dd:HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return null;
            }

            return parsed;
        }
    }

    public enum ResultType
    {
        Tightening = 1,
        Loosening = 2,
        BatchIncrement = 3,
        BatchDecrement = 4,
        Bypass = 5,
        Abort = 6,
        SyncTightening = 7,
        Reference = 8
    }

    public enum ResultPFTighteningStatus
    {
        NOk = 0,
        Ok = 1
    }

    public enum ResultPFValueSatus
    {
        Low = 0,
        OK = 1,
        High = 2
    }
}
