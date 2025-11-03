namespace OpenProtocol.OpenProtocolTypes
{
    public struct MultipleSpindleResult
    {
        public int NumberOfSpindles;
        public string VinNumber;
        public int JobId;
        public int PsetID;
        public int BatchSize;
        public int BatchCounter;
        public int BatchStatus;
        public float TorqueMinLimit;
        public float TorqueMaxLimit;
        public float TorqueFinalTarget;
        public int AngleMin;
        public int AngleMax;
        public int FinalAngleTarget;
        public string TimeStamp;
        public int SyngTightId;
        public bool SyncOverallStatus;
        public List<SpindleStatus> SpindlesStatus;
    }
}