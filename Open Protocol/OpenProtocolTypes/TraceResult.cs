using System.Globalization;
using System.Xml.Serialization;

namespace OpenProtocol.OpenProtocolTypes
{
    [XmlRoot("Trace Result", Namespace = "http://www.atlascopco.com",
        IsNullable = false)]
    public class TraceResult
    {
        public TraceType Tracetype { get; set; }
        public DateTime TimeStamp { get; set; }
        public uint ResultDataIdentifier { get; set; }
        public short NumberOfDatafields { get; set; }
        public List<DataField> Datafields { get; set; } = new();
        public short TransducerType { get; set; }
        public Unit Unit { get; set; }
        public short NumberOfParameters { get; set; }
        public List<DataField> ParametersField { get; set; } = new();
        public short ResolutionfieldsCount { get; set; }
        public List<ResolutionsFields> ResolutionFields { get; set; } = new();
        public int NumberOfSample { get; set; }
        public List<Double> SampleValues { get; set; } = new();

        public TraceResult(string Message, List<short> TraceData)
        {
            ParametersField = new List<DataField>();
            ResolutionFields = new List<ResolutionsFields>();
            SampleValues = new List<double>();
            float _coef = 0;
        
            string MessWithoutHeader = Message.Substring(20, Message.Length - 21);
            this.ResultDataIdentifier = uint.Parse(MessWithoutHeader.Substring(0, 10));
            try
            {
                this.TimeStamp = DateTime.ParseExact(MessWithoutHeader.Substring(10, 19), "yyyy-MM-dd:HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw;
            }


            this.NumberOfDatafields = short.Parse(MessWithoutHeader.Substring(29, 3));

            #region Datafields

            int PIDLength = 0;
            if (NumberOfDatafields > 0)
            {
                Datafields = new List<DataField>();
                int DataLength = short.Parse(MessWithoutHeader.Substring(37, 3));
                int offset = 0; // Offset du premier datafield
                for (short i = 0; i < NumberOfDatafields; i++)
                {
                    Datafields.Add(new DataField(MessWithoutHeader.Substring(32 + offset, 17 + DataLength)));
                    offset = offset + (DataLength + 17);
                    if (i != NumberOfDatafields - 1)
                        DataLength = short.Parse(MessWithoutHeader.Substring(37 + offset, 3));
                }

                PIDLength = offset;
            }

            #endregion

            string test = MessWithoutHeader.Substring(32 + PIDLength, 2);
            this.Tracetype = (TraceType)int.Parse(MessWithoutHeader.Substring(32 + PIDLength, 2));
            this.TransducerType = short.Parse(MessWithoutHeader.Substring(34 + PIDLength, 2));
            this.Unit = (Unit)short.Parse(MessWithoutHeader.Substring(36 + PIDLength, 3));
            this.NumberOfParameters = short.Parse(MessWithoutHeader.Substring(39 + PIDLength, 3));

            #region Parametersfields
            
            if (NumberOfParameters > 0)
            {
                int DataLength = short.Parse(MessWithoutHeader.Substring(47 + PIDLength, 3));
                int offset = 0; // Offset du premier datafield
                for (short i = 0; i < NumberOfParameters; i++)
                {
                    ParametersField.Add(
                        new DataField(MessWithoutHeader.Substring(42 + PIDLength + offset, 17 + DataLength)));
                    offset = offset + (DataLength + 17);

                    if (i != NumberOfParameters - 1)
                        DataLength = short.Parse(MessWithoutHeader.Substring(47 + PIDLength + offset, 3));
                }

                PIDLength = PIDLength + offset;
            }

            #endregion Parametersfields

            this.ResolutionfieldsCount = short.Parse(MessWithoutHeader.Substring(42 + PIDLength, 3));

            #region Resolutionsfields

            if (ResolutionfieldsCount > 0)
            {
               
                int DataLength = short.Parse(MessWithoutHeader.Substring(55 + PIDLength, 3));
                int offset = 0; // Offset du premier datafield
                for (short i = 0; i < ResolutionfieldsCount; i++)
                {
                    ResolutionFields.Add(
                        new ResolutionsFields(MessWithoutHeader.Substring(45 + PIDLength + offset, 18 + DataLength)));
                    offset = offset + (DataLength + 18);
                    if (i != ResolutionfieldsCount - 1)
                        DataLength = short.Parse(MessWithoutHeader.Substring(55 + PIDLength + offset, 3));
                }

                PIDLength = PIDLength + offset;
            }

            #endregion Parametersfields

            this.NumberOfSample = int.Parse(MessWithoutHeader.Substring(MessWithoutHeader.Length - 5, 5));

            if (ParametersField.Exists(x => x.PID == PIDEnum.CoefficientDiv))
            {
                if (ParametersField != null)
                {
                    var _param = ParametersField.Find(x => x.PID == PIDEnum.CoefficientDiv);
                    if (_param != null)
                    {
                        _coef = float.Parse(_param.DataValue,CultureInfo.InvariantCulture);
                    }
                }

                foreach (var Sample in TraceData)
                {

                    SampleValues.Add(Math.Round(Sample / _coef, 3));

                }

            }
            else if (ParametersField.Exists(x => x.PID == PIDEnum.CoefficientMul))
            {
                if (ParametersField != null)
                {
                    var _param = ParametersField.Find(x => x.PID == PIDEnum.CoefficientMul);
                    if (_param != null)
                    {
                        _coef = float.Parse(_param.DataValue,CultureInfo.InvariantCulture);
                    }
                }

                foreach (var Sample in TraceData)
                {

                    SampleValues.Add(Math.Round(Sample * _coef, 3));

                }
            }
        }

    }

    //[Serializable]
    //public class SpecialValue
    //{
    //    public int PID { get;  set; }
    //    public short DataLength { get;  set; }
    //    public short DataType { get;  set; }
    //    public Unit Unit { get;  set; }
    //    public short StepNumber { get; set; }
    //    public string DataValue { get; set; }
    //    public string Rawdata { get;  set; }

    //    public SpecialValue()
    //    { }
    //    public SpecialValue(string Data)
    //    {
    //        this.Rawdata = Data;
    //        this.PID =int.Parse( Data.Substring(0, 5));
    //        this.DataLength =short.Parse( Data.Substring(5, 3));
    //        this.DataType = short.Parse(Data.Substring(8, 2));
    //        this.Unit =(Unit) short.Parse(Data.Substring(10, 3));
    //        this.StepNumber = short.Parse(Data.Substring(13, 4));
    //        this.DataValue = Data.Substring(17, DataLength);
    //    }
    //}

    public enum TraceType
    {
        Angle = 1,
        Torque,
        Current,
        Gradient,
        Stroke,
        Force
    }

    public enum Unit
    {
        None = 0,
        Nm = 1,
        FtLbf = 2,
        cNm = 3,
        kNm = 4,
        MNm = 5,
        inLbf = 6,
        Kpm = 7,
        Kfcnm = 8,
        Percent = 9,
        Ozfin = 10,
        dNm = 11,
        mNm = 12,
        Deg = 50,
        rad = 51,
        Hz = 100,
        rpm = 101,
        Nmdeg = 150,
        NmRad = 160,
        s = 200,
        Min = 201,
        ms = 202,
        h = 203,
        N = 300,
        kN = 301,
        lbf = 302,
        kgf = 303,
        ozf = 304,
        MN = 305,
        m = 350,
        mm = 351,
        inc = 352
    }


    [Serializable]
    public class ResolutionsFields
    {
        public int FirstIndex { get; set; }
        public int LastIndex { get; set; }
        public short Lenght { get; set; }
        public short DatasType { get; set; }
        public Unit Unit { get; set; }
        public string TimeValue { get; set; } = string.Empty;


        public ResolutionsFields()
        {
        }

        public ResolutionsFields(string Data)
        {
            this.FirstIndex = int.Parse(Data.Substring(0, 5));
            this.LastIndex = int.Parse(Data.Substring(5, 5));
            this.Lenght = short.Parse(Data.Substring(10, 3));
            this.DatasType = short.Parse(Data.Substring(13, 2));
            this.Unit = (Unit)short.Parse(Data.Substring(15, 3));
            this.TimeValue = Data.Substring(18, Lenght);
        }
    }

    public class DataField
    {
        public PIDEnum PID { get; set; }
        public short Length { get; set; }
        public DataType DataType { get; set; }
        public short Unit { get; set; }
        public short Step { get; set; }
        public string DataValue { get; set; } = string.Empty;

        public DataField()
        {
        }

        public DataField(string Rawdata)
        {
            int PIDNum = int.Parse(Rawdata.Substring(0, 5));
            if (Enum.IsDefined(typeof(PIDEnum), PIDNum))
            {
                PID = (PIDEnum)PIDNum;
            }
            else
            {
                PID = PIDEnum.Undefined;
            }


            Length = short.Parse(Rawdata.Substring(5, 3));
            DataType = (DataType)int.Parse(Rawdata.Substring(8, 2));
            Unit = short.Parse(Rawdata.Substring(10, 3));
            Step = short.Parse(Rawdata.Substring(13, 4));
            DataValue = Rawdata.Substring(17, Length).Trim();
        }
    }

    public enum PIDEnum
    {
        TighteningStatus = 1,
        StationId = 2,
        StationName = 3,
        WpId = 10,
        Id1 = 11,
        Id2 = 12,
        Id3 = 13,
        Id4 = 14,
        Id5 = 5,
        Id6 = 6,
        Id7 = 7,
        Id8 = 8,
        Id9 = 9,
        TightID = 30,
        IDhandling = 31,
        Events = 40,
        OldestResID = 50,
        LastestResID = 51,
        OldestResulttTime = 52,
        LatestResultTime = 53,
        PSetNumber = 1000,
        PsetName = 1001,
        ControlStrategy = 1002,
        PsetTime = 1003,
        NumberOfStep = 1004,
        ControlerName = 1100,
        ControlerNumber = 1101,
        ControlerType = 1102,
        ControlerArticle = 1103,
        ControlerSerial = 1104,
        ToolName = 1200,
        ToolArticle = 1201,
        ToolSerial = 1202,
        ToolType = 1203,
        ToolTightenings = 1210,
        ToolTighService = 1211,
        TightToNextServ = 1212,
        BoltName = 1300,
        BoltNumber = 1301,
        BoltStatus = 1302,
        TighteningGenStatus = 1400,
        TighteningError = 1401,
        TorqueStatus = 1402,
        AngleStatus = 1403,
        RundownStatus = 1404,
        StationNumber = 1504,
        TorqueTarget = 2000,
        TFinalUppLim = 2002,
        TFinalLowLimit = 2003,
        AngleTarget = 2010,
        CoefficientDiv = 2213,
        CoefficientMul = 2214,
        TstepTarget = 5100,
        TStepUppLim = 5102,
        TStepLowLim = 5103,
        AStepTarget = 5110,
        AStepUppLim = 5113,
        AStepLowLim = 5114,


        StepForceTarget = 5130,
        StepForceValue = 5131,
        StepForceUppLimit = 5132,
        StepForceLowLimit = 5133,

        StepStrokeTarget = 5140,
        StepStrokeValue = 5141,
        StepStrokeUppLimit = 5142,
        StepStrokeLowLimit = 5143,


        StepStart = 5150,
        StepStop = 5151,
        Undefined = 9999
    }
}