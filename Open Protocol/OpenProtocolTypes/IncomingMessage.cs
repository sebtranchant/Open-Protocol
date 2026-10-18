namespace OpenProtocol.OpenProtocolTypes
{
    public sealed class IncomingMessage
    {
        public IncomingMessage(string ascii, IReadOnlyList<short>? curveSamples = null)
        {
            Ascii = ascii ?? string.Empty;
            Mid = Ascii.Length >= 8 ? Ascii.Substring(4, 4) : string.Empty;
            Revision = Ascii.Length >= 11 ? Ascii.Substring(8, 3) : string.Empty;
            CurveSamples = curveSamples ?? Array.Empty<short>();
        }

        public string Ascii { get; }
        public string Mid { get; }
        public string Revision { get; }
        public IReadOnlyList<short> CurveSamples { get; }
    }
}
