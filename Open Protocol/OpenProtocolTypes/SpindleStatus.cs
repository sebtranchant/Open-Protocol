namespace OpenProtocol.OpenProtocolTypes
{
    public struct SpindleStatus
    {
        public int SpindleNumber;
        public int ChanelID;
        public bool IndTightStatus;
        public bool IndTorqueStatus;
        public float TorqueResult;
        public bool IndAngleStatus;
        public int AngleResult;
    }
}