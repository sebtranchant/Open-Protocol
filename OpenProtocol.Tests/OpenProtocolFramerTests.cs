using System.Text;
using OpenProtocol.Services;

namespace OpenProtocol.Tests
{
    public class OpenProtocolFramerTests
    {
        [Fact]
        public void ExtractsTwoConcatenatedKeepAlives()
        {
            var keepAlive = Encoding.ASCII.GetBytes("00209999000         ");
            var buffer = new List<byte>();
            buffer.AddRange(keepAlive);
            buffer.Add(0);
            buffer.AddRange(keepAlive);
            buffer.Add(0);

            Assert.True(OpenProtocolFramer.TryExtract(buffer, out var first));
            Assert.Equal("9999", first!.Mid);
            Assert.True(OpenProtocolFramer.TryExtract(buffer, out var second));
            Assert.Equal("9999", second!.Mid);
            Assert.Empty(buffer);
        }

        [Fact]
        public void DoesNotExtractTruncatedFrame()
        {
            var buffer = Encoding.ASCII.GetBytes("00209999").ToList();

            Assert.False(OpenProtocolFramer.TryExtract(buffer, out var message));
            Assert.Null(message);
            Assert.Equal(8, buffer.Count);
        }

        [Fact]
        public void ExtractsMid0900BinaryIncludingEmbeddedNuls()
        {
            var ascii = "00250900001         00002";
            var buffer = Encoding.ASCII.GetBytes(ascii).ToList();
            buffer.Add(0);
            buffer.AddRange(new byte[] { 0x00, 0x01, 0xFF, 0x00 });

            Assert.True(OpenProtocolFramer.TryExtract(buffer, out var message));
            Assert.Equal("0900", message!.Mid);
            Assert.Equal(new short[] { 1, -256 }, message.CurveSamples);
            Assert.Empty(buffer);
        }

        [Fact]
        public void WaitsForRemaining0900Samples()
        {
            var ascii = "00250900001         00002";
            var buffer = Encoding.ASCII.GetBytes(ascii).ToList();
            buffer.Add(0);
            buffer.Add(0x00);

            Assert.False(OpenProtocolFramer.TryExtract(buffer, out var message));
            Assert.Null(message);
            Assert.True(buffer.Count > 0);
        }

        [Fact]
        public void CommandErrorMid0004IsACompleteFrame()
        {
            var ascii = OpMessageBuild("0004");
            var buffer = Encoding.ASCII.GetBytes(ascii).ToList();
            buffer.Add(0);

            Assert.True(OpenProtocolFramer.TryExtract(buffer, out var message));
            Assert.Equal("0004", message!.Mid);
        }

        private static string OpMessageBuild(string mid)
        {
            return "0020" + mid + "000         ";
        }
    }
}
