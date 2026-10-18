using System.Globalization;
using System.Text;
using OpenProtocol.OpenProtocolTypes;

namespace OpenProtocol.Services
{
    /// <summary>
    /// Splits an Open Protocol byte stream into telegrams.
    /// Frames are length-prefixed (length includes the 4 length bytes, excludes the trailing NUL).
    /// MID 0900 may append big-endian Int16 samples after the NUL.
    /// A binary tail larger than <see cref="MaxBinaryBytes"/> is dropped and the scan continues.
    /// </summary>
    public static class OpenProtocolFramer
    {
        public const int HeaderSize = 20;
        public const int MaxAsciiLength = 9999;
        public const int MaxBinaryBytes = 1024 * 1024;
        public const byte MessageTerminator = 0x00;

        public static bool TryExtract(List<byte> buffer, out IncomingMessage? message)
        {
            return TryExtract(buffer, out message, MaxBinaryBytes);
        }

        internal static bool TryExtract(List<byte> buffer, out IncomingMessage? message, int maxBinaryBytes)
        {
            message = null;
            while (buffer.Count >= 4)
            {
                var lengthText = Encoding.ASCII.GetString(buffer.GetRange(0, 4).ToArray());
                if (!int.TryParse(lengthText, NumberStyles.None, CultureInfo.InvariantCulture, out int asciiLength)
                    || asciiLength < HeaderSize
                    || asciiLength > MaxAsciiLength)
                {
                    buffer.RemoveAt(0);
                    continue;
                }

                if (buffer.Count < asciiLength)
                {
                    return false;
                }

                var ascii = Encoding.ASCII.GetString(buffer.GetRange(0, asciiLength).ToArray());
                var mid = ascii.Length >= 8 ? ascii.Substring(4, 4) : string.Empty;

                var consumed = asciiLength;
                if (buffer.Count > asciiLength && buffer[asciiLength] == MessageTerminator)
                {
                    consumed = asciiLength + 1;
                }

                var curveSamples = new List<short>();
                if (mid == "0900")
                {
                    var sampleCount = ParseSampleCount(ascii);
                    var binaryBytes = (long)sampleCount * 2;
                    if (binaryBytes > maxBinaryBytes)
                    {
                        buffer.RemoveRange(0, consumed);
                        continue;
                    }

                    if (buffer.Count < consumed + binaryBytes)
                    {
                        return false;
                    }

                    curveSamples = ReadBigEndianInt16(buffer, consumed, sampleCount);
                    consumed += (int)binaryBytes;
                }

                buffer.RemoveRange(0, consumed);
                message = new IncomingMessage(ascii, curveSamples);
                return true;
            }

            return false;
        }

        private static int ParseSampleCount(string ascii)
        {
            if (ascii.Length < 5)
            {
                return 0;
            }

            return int.TryParse(
                ascii.AsSpan(ascii.Length - 5, 5),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int sampleCount)
                ? Math.Max(sampleCount, 0)
                : 0;
        }

        private static List<short> ReadBigEndianInt16(List<byte> buffer, int offset, int sampleCount)
        {
            var samples = new List<short>(sampleCount);
            for (var i = 0; i < sampleCount; i++)
            {
                var index = offset + i * 2;
                samples.Add((short)((buffer[index] << 8) | buffer[index + 1]));
            }

            return samples;
        }
    }
}
