using System.Xml.Serialization;

namespace OpenProtocol.OpenProtocolTypes
{
    [XmlRoot("Bolt Result", Namespace = "http://www.atlascopco.com",
        IsNullable = false)]
    [Serializable]
    public class ResultBolt
    {
        [NonSerialized] public int NumOfMessages;
        [NonSerialized] public int MessageNumber;
        [NonSerialized] public int IDData;
        public int StationNumber;
        public string Time = "";
        public int BoltNumber;
        public string BoltName = "";
        public string PsetName = "";
        public int PMStatus;
        public string Errors = "";
        public string CustomErrors = "";
        public int NumOfBoltResults;
        public List<BoltVar> BoltsVar = new();
        public int NumOfStepResults;
        public List<StepResult> StepResults = new();
        public int NumOfSpecialBoltValue;
        public List<SpecialValue> SpecialBoltValues = new();

        public ResultBolt()
        {
        }

        public ResultBolt(string rawdata)
        {
            BoltsVar = new List<BoltVar>();
            StepResults = new List<StepResult>();
            this.NumOfMessages = int.Parse(rawdata.Substring(22, 2));
            this.MessageNumber = int.Parse(rawdata.Substring(26, 2));
            this.IDData = int.Parse(rawdata.Substring(30, 10));
            this.StationNumber = int.Parse(rawdata.Substring(42, 2));
            this.Time = rawdata.Substring(46, 19);
            this.BoltNumber = int.Parse(rawdata.Substring(67, 4));
            this.BoltName = rawdata.Substring(73, 20).Trim();
            this.PsetName = rawdata.Substring(95, 20).Trim();
            this.PMStatus = int.Parse(rawdata.Substring(119, 1));
            this.Errors = rawdata.Substring(120, 50);
            this.CustomErrors = rawdata.Substring(172, 4);
            this.NumOfBoltResults = int.Parse(rawdata.Substring(178, 2));

            int BoltResultoffset = 0;

            #region Bolt Result

            for (int resultNumber = 0; resultNumber < this.NumOfBoltResults; resultNumber++)
            {
                BoltResultoffset = 180 + (resultNumber * 29);

                this.BoltsVar.Add(new BoltVar(rawdata.Substring(BoltResultoffset, 29)));
            }

            #endregion

            int StepOffset = BoltResultoffset + 31;
            this.NumOfStepResults = int.Parse(rawdata.Substring(StepOffset, 3));
            int Step = 0;

            #region StepResult

            if (NumOfStepResults > 0)
            {
                StepOffset = StepOffset + 6;
                for (int StepResNum = 0; StepResNum < NumOfStepResults; StepResNum++)
                {
                    Step = StepOffset + (StepResNum * 31);
                    StepResults.Add(new StepResult(rawdata.Substring(Step, 31)));
                }
            }

            #endregion

            int SpecialValueOffset = Step + 33;
            this.NumOfSpecialBoltValue = int.Parse(rawdata.Substring(SpecialValueOffset, 2));

            #region SpecialValue

            if (this.NumOfSpecialBoltValue > 0)
            {
                SpecialBoltValues = new List<SpecialValue>();
                SpecialValueOffset = SpecialValueOffset + 2; // Offset du premier SpecialValue

                for (short i = 0; i < NumOfSpecialBoltValue; i++)
                {
                    int SVLenght = int.Parse(rawdata.Substring(SpecialValueOffset + 22, 2));

                    SpecialBoltValues.Add(new SpecialValue(rawdata.Substring(SpecialValueOffset, SVLenght + 25)));
                    SpecialValueOffset = SpecialValueOffset + (SVLenght + 26);
                }
            }

            #endregion
        }
    }

    [XmlRoot("Fitting Result", Namespace = "http://www.atlascopco.com",
        IsNullable = false)]
    [Serializable]
    public class ResultFitting
    {
        [NonSerialized] public int NumOfMessages;
        [NonSerialized] public int MessageNumber;
        [NonSerialized] public int IDData;
        public int StationNumber;
        public string Time = string.Empty;
        public int FittingNumber;
        public string FittingName = string.Empty;
        public string PsetName = string.Empty;
        public int PMStatus;
        public string Errors = string.Empty;
        public string CustomErrors = string.Empty;
        public int NumOfFittingResults;
        public List<BoltVar> FittingsVar = new();
        public int NumOfStepResults;
        public List<StepResult> StepResults = new();
        public int NumOfSpecialBoltValue;
        public List<SpecialValue> SpecialBoltValues = new();

        public ResultFitting()
        {
        }

        public ResultFitting(string rawData)
        {
            FittingsVar = new List<BoltVar>();
            StepResults = new List<StepResult>();
            this.NumOfMessages = int.Parse(rawData.Substring(22, 2));
            this.MessageNumber = int.Parse(rawData.Substring(26, 2));
            this.IDData = int.Parse(rawData.Substring(30, 10));
            this.StationNumber = int.Parse(rawData.Substring(42, 2));
            this.Time = rawData.Substring(46, 19);
            this.FittingNumber = int.Parse(rawData.Substring(67, 4));
            this.FittingName = rawData.Substring(73, 20).Trim();
            this.PsetName = rawData.Substring(95, 20).Trim();
            this.PMStatus = int.Parse(rawData.Substring(119, 1));
            this.Errors = rawData.Substring(120, 50);
            this.CustomErrors = rawData.Substring(172, 4);
            this.NumOfFittingResults = int.Parse(rawData.Substring(178, 2));

            int BoltResultoffset = 0;

            #region Bolt Result

            for (int resultNumber = 0; resultNumber < this.NumOfFittingResults; resultNumber++)
            {
                BoltResultoffset = 180 + (resultNumber * 29);

                this.FittingsVar.Add(new BoltVar(rawData.Substring(BoltResultoffset, 29)));
            }

            #endregion

            int StepOffset = BoltResultoffset + 31;
            this.NumOfStepResults = int.Parse(rawData.Substring(StepOffset, 3));
            int Step = 0;

            #region StepResult

            if (NumOfStepResults > 0)
            {
                StepOffset = StepOffset + 6;
                for (int StepResNum = 0; StepResNum < NumOfStepResults; StepResNum++)
                {
                    Step = StepOffset + (StepResNum * 31);
                    StepResults.Add(new StepResult(rawData.Substring(Step, 31)));
                }
            }

            #endregion

            int SpecialValueOffset = Step + 33;
            this.NumOfSpecialBoltValue = int.Parse(rawData.Substring(SpecialValueOffset, 2));

            #region SpecialValue

            if (this.NumOfSpecialBoltValue > 0)
            {
                SpecialBoltValues = new List<SpecialValue>();
                SpecialValueOffset = SpecialValueOffset + 2; // Offset du premier SpecialValue

                for (short i = 0; i < NumOfSpecialBoltValue; i++)
                {
                    int SVLenght = int.Parse(rawData.Substring(SpecialValueOffset + 22, 2));

                    SpecialBoltValues.Add(new SpecialValue(rawData.Substring(SpecialValueOffset, SVLenght + 25)));
                    SpecialValueOffset = SpecialValueOffset + (SVLenght + 26);
                }
            }

            #endregion
        }
    }

    public class BoltVar
    {
        public string VarName = "";
        public string VarType = "";
        public string VarValue = "";

        public BoltVar()
        {
        }

        public BoltVar(string boltRawData)
        {
            VarName = boltRawData.Substring(0, 20).Trim();
            VarType = boltRawData.Substring(20, 2).Trim();
            VarValue = boltRawData.Substring(22, 7).Trim();
        }
    }

    public class FittingVar
    {
        public string VarName = string.Empty;
        public string VarType = string.Empty;
        public string VarValue = string.Empty;

        public FittingVar()
        {
        }

        public FittingVar(string boltRawData)
        {
            VarName = boltRawData.Substring(0, 20).Trim();
            VarType = boltRawData.Substring(20, 2).Trim();
            VarValue = boltRawData.Substring(22, 7).Trim();
        }
    }

    public class StepResult
    {
        public string VariableName = string.Empty;
        public string Type = string.Empty;
        public string Value = string.Empty;
        public short StepNumber;

        public StepResult()
        {
        }

        public StepResult(string StepRawdata)
        {
            VariableName = StepRawdata.Substring(0, 20).Trim();
            Type = StepRawdata.Substring(20, 2).Trim();
            Value = StepRawdata.Substring(22, 7);
            try
            {
                StepNumber = short.Parse(StepRawdata.Substring(29, 2));
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}