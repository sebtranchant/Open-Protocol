namespace OpenProtocol.OpenProtocolTypes
{
    public class JobEvtArgs : EventArgs
    {
        private string _MyEventText;

        public JobStatus JobState
        {
            get { return (JobStatus)int.Parse(_MyEventText.Substring(28, 1)); }
        }

        public int JobId
        {
            get { return int.Parse(_MyEventText.Substring(22, 4)); }
        }

        public int JobBatch
        {
            get { return int.Parse(_MyEventText.Substring(34, 4)); }
        }

        public int JobCount
        {
            get { return int.Parse(_MyEventText.Substring(40, 4)); }
        }


        public JobEvtArgs(string JobMessage)
        {
            _MyEventText = JobMessage;
        }
    }

    public enum JobStatus
    {
        NotCompleted = 0,
        OK = 1,
        NOK = 2,
        ABORT = 3
    };
}