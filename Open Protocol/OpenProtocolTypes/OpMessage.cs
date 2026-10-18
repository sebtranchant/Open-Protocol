using System.Globalization;

namespace OpenProtocol.OpenProtocolTypes
{
    public static class OpMessage
    {
        public const int HeaderSize = 20;

        public static string Build<TEnum>(TEnum messageId, short revision, string data)
            where TEnum : Enum
        {
            return Build(Convert.ToInt32(messageId, CultureInfo.InvariantCulture), revision, data);
        }

        public static string Build(int messageId, short revision, string data)
        {
            data ??= string.Empty;
            var totalLength = HeaderSize + data.Length;
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:D4}{1:D4}{2:D3}         {3}",
                totalLength,
                messageId,
                revision,
                data);
        }

        public static string GetMid(string ascii)
        {
            return ascii.Length >= 8 ? ascii.Substring(4, 4) : string.Empty;
        }
    }
}
