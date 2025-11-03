namespace OpenProtocol.OpenProtocolTypes
{
    public class MutipleSpindlesResultsEvtArgs : EventArgs
    {
        private SpindleStatus spindleStatus;

        private MultipleSpindleResult multipleSpindleResult;

        public MultipleSpindleResult MultipleSplindesResultEvt
        {
            get { return multipleSpindleResult; }
        }

        public MutipleSpindlesResultsEvtArgs(string Rawdata)
        {
            multipleSpindleResult.NumberOfSpindles = int.Parse(Rawdata.Substring(23, 1));
            multipleSpindleResult.VinNumber = Rawdata.Substring(27, 25);
            multipleSpindleResult.JobId = int.Parse(Rawdata.Substring(54, 1));
            multipleSpindleResult.PsetID = int.Parse(Rawdata.Substring(58, 3));
            multipleSpindleResult.BatchSize = int.Parse(Rawdata.Substring(63, 4));
            multipleSpindleResult.BatchCounter = int.Parse(Rawdata.Substring(69, 4));
            multipleSpindleResult.BatchStatus = int.Parse(Rawdata.Substring(75, 1));
            multipleSpindleResult.TorqueMinLimit = float.Parse(Rawdata.Substring(78, 6)) / 100;
            multipleSpindleResult.TorqueMaxLimit = float.Parse(Rawdata.Substring(86, 6)) / 100;
            multipleSpindleResult.TorqueFinalTarget = float.Parse(Rawdata.Substring(94, 6)) / 100;
            multipleSpindleResult.AngleMin = int.Parse(Rawdata.Substring(102, 5));
            multipleSpindleResult.AngleMax = int.Parse(Rawdata.Substring(109, 5));
            multipleSpindleResult.FinalAngleTarget = int.Parse(Rawdata.Substring(116, 5));
            multipleSpindleResult.TimeStamp = Rawdata.Substring(144, 19);
            multipleSpindleResult.SyngTightId = int.Parse(Rawdata.Substring(165, 5));
            //      multipleSpindleResult.SyncOverallStatus = bool.Parse(Rawdata.Substring(172,1));

            for (int spindlenumber = 0; spindlenumber < multipleSpindleResult.NumberOfSpindles; spindlenumber++)
            {
                int offset = 175 + 18 * spindlenumber;
                spindleStatus.SpindleNumber = int.Parse(Rawdata.Substring(offset, 2));
                spindleStatus.ChanelID = int.Parse(Rawdata.Substring(offset + 3, 2));
                //  spindleStatus.IndTightStatus = bool.Parse (Rawdata.Substring(offset+5,1));
                //  spindleStatus.IndTorqueStatus = bool.Parse(Rawdata.Substring(offset+6,1));
                spindleStatus.TorqueResult = float.Parse(Rawdata.Substring(offset + 7, 4));
                //    spindleStatus.IndAngleStatus = bool.Parse(Rawdata.Substring(offset + 13, 1));
                spindleStatus.AngleResult = int.Parse(Rawdata.Substring(offset + 14, 5));
                multipleSpindleResult.SpindlesStatus.Add(spindleStatus);
            }
        }
    }
}