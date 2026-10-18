using OpenProtocol.OpenProtocolTypes;

namespace OpenProtocol.Tests
{
    public class OpMessageTests
    {
        [Fact]
        public void Build_EmptyData_IsTwentyByteHeaderWithoutNul()
        {
            var message = OpMessage.Build(Mids.KeepAlive, 0, "");

            Assert.Equal(20, message.Length);
            Assert.Equal("0020", message[..4]);
            Assert.Equal("9999", message[4..8]);
            Assert.Equal("000", message[8..11]);
            Assert.DoesNotContain('\0', message);
        }

        [Fact]
        public void Build_IncludesDataInLength()
        {
            var message = OpMessage.Build(Mids.CommandAcknowledgment, 1, "0900");

            Assert.Equal("0024", message[..4]);
            Assert.Equal("0005", message[4..8]);
            Assert.Equal("001", message[8..11]);
            Assert.Equal("0900", message[^4..]);
        }
    }
}
