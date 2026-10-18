using System.Globalization;

namespace OpenProtocol.OpenProtocolTypes
{
    public class JobEvtArgs : EventArgs
    {
        public JobStatus JobState { get; }
        public int JobId { get; }
        public int JobBatch { get; }
        public int JobCount { get; }

        public JobEvtArgs(string JobMessage)
        {
            if (string.IsNullOrEmpty(JobMessage))
            {
                throw new FormatException("Job info message is empty.");
            }

            var revision = ReadInt(JobMessage, 8, 3);
            var jobIdLength = revision <= 1 ? 2 : 4;
            var statusAt = 22 + jobIdLength + 2;
            var batchAt = statusAt + 1 + 2 + 1 + 2;
            var countAt = batchAt + 4 + 2;

            JobId = ReadInt(JobMessage, 22, jobIdLength);
            var status = ReadInt(JobMessage, statusAt, 1);
            if (!Enum.IsDefined(typeof(JobStatus), status))
            {
                throw new FormatException($"Job status '{status}' is not defined.");
            }

            JobState = (JobStatus)status;
            JobBatch = ReadInt(JobMessage, batchAt, 4);
            JobCount = ReadInt(JobMessage, countAt, 4);
        }

        private static int ReadInt(string message, int start, int length)
        {
            if (start < 0 || message.Length < start + length)
            {
                throw new FormatException("Job info message is too short.");
            }

            if (!int.TryParse(message.AsSpan(start, length), NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                throw new FormatException("Job info field is not numeric.");
            }

            return value;
        }
    }

    public enum JobStatus
    {
        NotCompleted = 0,
        OK = 1,
        NOK = 2,
        ABORT = 3
    }
}
