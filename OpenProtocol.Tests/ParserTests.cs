using OpenProtocol.OpenProtocolTypes;

namespace OpenProtocol.Tests
{
    public class ParserTests
    {
        [Fact]
        public void ShortCommunicationStartAckDoesNotThrow()
        {
            var info = new ControllerInformation(new string('x', 40));

            Assert.Equal(string.Empty, info.ControllerName);
            Assert.Equal(SystemTypeEnum.NotSet, info.SysType);
            Assert.Equal(0, info.CellID);
        }

        [Fact]
        public void ResultDateUsesTwentyFourHourClock()
        {
            var raw = Stamp(364, 345, "2024-06-01:15:04:05");

            var parsed = new ResultEvtArgs(raw).Date;

            Assert.Equal(new DateTime(2024, 6, 1, 15, 4, 5), parsed);
        }

        [Fact]
        public void UnreadableResultDateIsNull()
        {
            var raw = Stamp(364, 345, new string('Z', 19));

            Assert.Null(new ResultEvtArgs(raw).Date);
            Assert.Throws<FormatException>(() => new ResultEvtArgs("0061"));
        }

        [Fact]
        public void ResultTorqueIsScaledAndMissingTailIsEmpty()
        {
            var raw = Stamp(320, 183, "001250");

            var parsed = new ResultEvtArgs(raw);

            Assert.Equal(12.5f, parsed.Couple);
            Assert.Equal(0, parsed.TightId);
            Assert.Null(parsed.Date);
            Assert.Equal(string.Empty, parsed.PsetName);
        }

        [Fact]
        public void ParameterSetDateReadsNineteenCharacters()
        {
            var raw = Stamp(42, 23, "2024-06-01:15:04:05");

            var parsed = new ResultParameterSetId(raw).Date;

            Assert.Equal(new DateTime(2024, 6, 1, 15, 4, 5), parsed);
        }

        [Fact]
        public void UserDataKeepsTheLastByte()
        {
            var message = new string('0', 20) + "AB";

            var parsed = new UserDataEvtArgs(message);

            Assert.Equal("AB", parsed.HexaValue);
            Assert.Equal(new byte[] { 0xAB }, parsed.Inputs);
        }

        [Fact]
        public void OddUserDataHexThrows()
        {
            var message = new string('0', 20) + "A";

            Assert.Throws<FormatException>(() => new UserDataEvtArgs(message));
        }

        [Fact]
        public void JobInfoRevision1UsesATwoDigitJobId()
        {
            var message = JobInfo(1, "0107021030040008050003");

            var parsed = new JobEvtArgs(message);

            Assert.Equal(7, parsed.JobId);
            Assert.Equal(JobStatus.OK, parsed.JobState);
            Assert.Equal(8, parsed.JobBatch);
            Assert.Equal(3, parsed.JobCount);
        }

        [Fact]
        public void JobInfoRevision4UsesAFourDigitJobId()
        {
            var message = JobInfo(4, "010015023031040020050006");

            var parsed = new JobEvtArgs(message);

            Assert.Equal(15, parsed.JobId);
            Assert.Equal(JobStatus.ABORT, parsed.JobState);
            Assert.Equal(20, parsed.JobBatch);
            Assert.Equal(6, parsed.JobCount);
        }

        [Fact]
        public void ShortJobInfoThrows()
        {
            Assert.Throws<FormatException>(() => new JobEvtArgs("0035"));
        }

        [Fact]
        public void SecondSpindleIsNotOverlappedByTheFirst()
        {
            var chars = Enumerable.Repeat('0', 213).ToArray();
            chars[23] = '2';
            Write(chars, 175, "01");
            Write(chars, 178, "07");
            chars[180] = '1';
            Write(chars, 182, "0200");
            chars[188] = '1';
            Write(chars, 189, "00015");
            Write(chars, 194, "02");

            var result = new MutipleSpindlesResultsEvtArgs(new string(chars)).MultipleSplindesResultEvt;

            Assert.Equal(2, result.SpindlesStatus.Count);
            Assert.Equal(1, result.SpindlesStatus[0].SpindleNumber);
            Assert.True(result.SpindlesStatus[0].IndTightStatus);
            Assert.Equal(2f, result.SpindlesStatus[0].TorqueResult);
            Assert.Equal(2, result.SpindlesStatus[1].SpindleNumber);
        }

        private static string JobInfo(int revision, string data)
        {
            return $"00000035{revision:000}         {data}";
        }

        private static string Stamp(int length, int index, string value)
        {
            var chars = Enumerable.Repeat('0', length).ToArray();
            value.CopyTo(chars.AsSpan(index));
            return new string(chars);
        }

        private static void Write(char[] chars, int index, string value)
        {
            value.CopyTo(chars.AsSpan(index));
        }
    }
}
