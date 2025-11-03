using System.Globalization;
using System.Xml.Serialization;

namespace OpenProtocol.OpenProtocolTypes
{
    [XmlRoot("Station Result", Namespace = "http://www.atlascopco.com", IsNullable = false)]
    [Serializable]
    public class TightResultStation
    {
        [NonSerialized] public int NumOfMessages;
        [NonSerialized] public int MessageNumber;
        [NonSerialized] public int IDData;
        public int StationNumber;
        public string StationName = string.Empty;
        public string Time = string.Empty;
        public int ModeNumber;
        public string ModeName = string.Empty;
        public int SimpleStatus;
        public int PMStatus;
        public string WpId = string.Empty;
        public int NumOfbolts;
        public int SystemSubType;
        public List<TighteningValue> BoltValues = new();
        public int NumberOfSpecialValues;
        public List<SpecialValue> SpecialValues = new();


        public TightResultStation()
        {
        }

        public TightResultStation(string rawdata)
        {
            BoltValues = new List<TighteningValue>();
            SpecialValues = new List<SpecialValue>();

            this.NumOfMessages = int.Parse(rawdata.Substring(22, 2));
            this.MessageNumber = int.Parse(rawdata.Substring(26, 2));
            this.IDData = int.Parse(rawdata.Substring(30, 10));
            this.StationNumber = int.Parse(rawdata.Substring(42, 2));
            this.StationName = rawdata.Substring(46, 20).Trim();
            this.Time = rawdata.Substring(68, 19);
            if (rawdata.Substring(89, 2) != "  ")
            {
                this.ModeNumber = int.Parse(rawdata.Substring(89, 2));
            }
            else
            {
                this.ModeNumber = 0;
            }

            this.ModeName = rawdata.Substring(93, 20).Trim();
            this.SimpleStatus = int.Parse(rawdata.Substring(115, 1));
            this.PMStatus = int.Parse(rawdata.Substring(118, 1));
            this.WpId = rawdata.Substring(121, 40).Trim();
            this.NumOfbolts = int.Parse(rawdata.Substring(163, 2));

            int Boltoffset = 0;

            #region BoltValues

            for (int BoltNumber = 0; BoltNumber < this.NumOfbolts; BoltNumber++)
            {
                TighteningValue boltvalue;
                Boltoffset = 167 + 67 * BoltNumber;
                boltvalue.BoltNumber = int.Parse(rawdata.Substring(Boltoffset, 2));
                boltvalue.SimpleBoltStatus = int.Parse(rawdata.Substring(Boltoffset + 4, 1));
                if (rawdata.Substring(Boltoffset + 7, 1) != " ")
                {
                    boltvalue.TorqueStatus = int.Parse(rawdata.Substring(Boltoffset + 7, 1));
                }
                else
                {
                    boltvalue.TorqueStatus = 3;
                }

                if (rawdata.Substring(Boltoffset + 10, 1) != " ")
                {
                    boltvalue.AngleStatus = int.Parse(rawdata.Substring(Boltoffset + 10, 1));
                }
                else
                {
                    boltvalue.AngleStatus = 3;
                }

                if (rawdata.Substring(Boltoffset + 13, 7) != "       ")
                {
                    boltvalue.BoltTorque =
                        float.Parse(rawdata.Substring(Boltoffset + 13, 7), CultureInfo.InvariantCulture);
                }
                else
                {
                    boltvalue.BoltTorque = 9999999;
                }

                if (rawdata.Substring(Boltoffset + 22, 7) != "       ")
                {
                    boltvalue.BoltAngle =
                        float.Parse(rawdata.Substring(Boltoffset + 22, 7), CultureInfo.InvariantCulture);
                }
                else
                {
                    boltvalue.BoltAngle = 9999999;
                }

                if (rawdata.Substring(Boltoffset + 31, 7) != "       ")
                {
                    boltvalue.BoltTorqueHighLimit = float.Parse(rawdata.Substring(Boltoffset + 31, 7),
                        CultureInfo.InvariantCulture);
                }
                else
                {
                    boltvalue.BoltTorqueHighLimit = 9999999;
                }

                if (rawdata.Substring(Boltoffset + 40, 7) != "       ")
                {
                    boltvalue.BoltTorqueLowLimit = float.Parse(rawdata.Substring(Boltoffset + 40, 7),
                        CultureInfo.InvariantCulture);
                }
                else
                {
                    boltvalue.BoltTorqueLowLimit = 9999999;
                }

                if (rawdata.Substring(Boltoffset + 49, 7) != "       ")
                {
                    boltvalue.BoltAngleHighLimit = float.Parse(rawdata.Substring(Boltoffset + 49, 7),
                        CultureInfo.InvariantCulture);
                }
                else
                {
                    boltvalue.BoltAngleHighLimit = 9999999;
                }

                if (rawdata.Substring(Boltoffset + 58, 7) != "       ")
                {
                    boltvalue.BoltAngleLowLimit = float.Parse(rawdata.Substring(Boltoffset + 58, 7),
                        CultureInfo.InvariantCulture);
                }
                else
                {
                    boltvalue.BoltAngleLowLimit = 9999999;
                }

                this.BoltValues.Add(boltvalue);
            }

            #endregion

            Boltoffset = Boltoffset + 67;
            this.NumberOfSpecialValues = int.Parse(rawdata.Substring(Boltoffset, 2));

            #region Special value

            int SpecialValueoffset = 0;

            if (this.NumberOfSpecialValues > 0)
            {
                SpecialValues = new List<SpecialValue>();
                SpecialValueoffset = Boltoffset + 2; // Offset du premier SpecialValue

                for (short i = 0; i < NumberOfSpecialValues; i++)
                {
                    int SVLenght = int.Parse(rawdata.Substring(SpecialValueoffset + 22, 2));

                    SpecialValues.Add(new SpecialValue(rawdata.Substring(SpecialValueoffset, SVLenght + 25)));
                    SpecialValueoffset = SpecialValueoffset + (SVLenght + 24);
                }
            }

            #endregion

            SystemSubType = int.Parse(rawdata.Substring(SpecialValueoffset + 2, 3));
        }
    }

    [XmlRoot("Station Result", Namespace = "http://www.atlascopco.com", IsNullable = false)]
    [Serializable]
    public class PressResultStation
    {
        [NonSerialized] public int NumOfMessages;
        [NonSerialized] public int MessageNumber;
        [NonSerialized] public int IDData;
        public int StationNumber;
        public string StationName = string.Empty;
        public string Time = string.Empty;
        public int ModeNumber;
        public string ModeName = string.Empty;
        public int SimpleStatus;
        public int PMStatus;
        public string WpId = string.Empty;
        public int NumOfFit;
        public int SystemSubType;
        public List<FitingValue> FitValues = new();
        public int NumberOfSpecialValues;
        public List<SpecialValue> SpecialValues = new();


        public PressResultStation()
        {
        }

        public PressResultStation(string rawdata)
        {
            FitValues = new List<FitingValue>();
            SpecialValues = new List<SpecialValue>();

            this.NumOfMessages = int.Parse(rawdata.Substring(22, 2));
            this.MessageNumber = int.Parse(rawdata.Substring(26, 2));
            this.IDData = int.Parse(rawdata.Substring(30, 10));
            this.StationNumber = int.Parse(rawdata.Substring(42, 2));
            this.StationName = rawdata.Substring(46, 20).Trim();
            this.Time = rawdata.Substring(68, 19);
            if (rawdata.Substring(89, 2) != "  ")
            {
                this.ModeNumber = int.Parse(rawdata.Substring(89, 2));
            }
            else
            {
                this.ModeNumber = 0;
            }

            this.ModeName = rawdata.Substring(93, 20).Trim();
            this.SimpleStatus = int.Parse(rawdata.Substring(115, 1));
            this.PMStatus = int.Parse(rawdata.Substring(118, 1));
            this.WpId = rawdata.Substring(121, 40).Trim();
            this.NumOfFit = int.Parse(rawdata.Substring(163, 2));

            int Boltoffset = 0;

            #region BoltValues

            for (int BoltNumber = 0; BoltNumber < this.NumOfFit; BoltNumber++)
            {
                FitingValue boltvalue;
                Boltoffset = 167 + 67 * BoltNumber;
                boltvalue.FittingNumber = int.Parse(rawdata.Substring(Boltoffset, 2));
                boltvalue.SimpleFittingStatus = int.Parse(rawdata.Substring(Boltoffset + 4, 1));
                if (rawdata.Substring(Boltoffset + 7, 1) != " ")
                {
                    boltvalue.ForceStatus = int.Parse(rawdata.Substring(Boltoffset + 7, 1));
                }
                else
                {
                    boltvalue.ForceStatus = 3;
                }

                if (rawdata.Substring(Boltoffset + 10, 1) != " ")
                {
                    boltvalue.StrokeStatus = int.Parse(rawdata.Substring(Boltoffset + 10, 1));
                }
                else
                {
                    boltvalue.StrokeStatus = 3;
                }

                if (rawdata.Substring(Boltoffset + 13, 7) != "       ")
                {
                    boltvalue.FittingForce =
                        float.Parse(rawdata.Substring(Boltoffset + 13, 7), CultureInfo.InvariantCulture);
                }
                else
                {
                    boltvalue.FittingForce = 9999999;
                }

                if (rawdata.Substring(Boltoffset + 22, 7) != "       ")
                {
                    boltvalue.FittingStroke =
                        float.Parse(rawdata.Substring(Boltoffset + 22, 7), CultureInfo.InvariantCulture);
                }
                else
                {
                    boltvalue.FittingStroke = 9999999;
                }

                if (rawdata.Substring(Boltoffset + 31, 7) != "       ")
                {
                    boltvalue.FittingForceHighLimit = float.Parse(rawdata.Substring(Boltoffset + 31, 7),
                        CultureInfo.InvariantCulture);
                }
                else
                {
                    boltvalue.FittingForceHighLimit = 9999999;
                }

                if (rawdata.Substring(Boltoffset + 40, 7) != "       ")
                {
                    boltvalue.FittingForceLowLimit = float.Parse(rawdata.Substring(Boltoffset + 40, 7),
                        CultureInfo.InvariantCulture);
                }
                else
                {
                    boltvalue.FittingForceLowLimit = 9999999;
                }

                if (rawdata.Substring(Boltoffset + 49, 7) != "       ")
                {
                    boltvalue.FittingStrokeHighLimit = float.Parse(rawdata.Substring(Boltoffset + 49, 7),
                        CultureInfo.InvariantCulture);
                }
                else
                {
                    boltvalue.FittingStrokeHighLimit = 9999999;
                }

                if (rawdata.Substring(Boltoffset + 58, 7) != "       ")
                {
                    boltvalue.FittingStrokeLowLimit = float.Parse(rawdata.Substring(Boltoffset + 58, 7),
                        CultureInfo.InvariantCulture);
                }
                else
                {
                    boltvalue.FittingStrokeLowLimit = 9999999;
                }

                this.FitValues.Add(boltvalue);
            }

            #endregion

            Boltoffset = Boltoffset + 67;
            this.NumberOfSpecialValues = int.Parse(rawdata.Substring(Boltoffset, 2));

            #region Special value

            int SpecialValueoffset = 0;

            if (this.NumberOfSpecialValues > 0)
            {
                SpecialValues = new List<SpecialValue>();
                SpecialValueoffset = Boltoffset + 2; // Offset du premier SpecialValue

                for (short i = 0; i < NumberOfSpecialValues; i++)
                {
                    int SVLenght = int.Parse(rawdata.Substring(SpecialValueoffset + 22, 2));

                    SpecialValues.Add(new SpecialValue(rawdata.Substring(SpecialValueoffset, SVLenght + 25)));
                    SpecialValueoffset = SpecialValueoffset + (SVLenght + 24);
                }
            }

            #endregion

            SystemSubType = int.Parse(rawdata.Substring(SpecialValueoffset + 2, 3));
        }
    }

    public class SpecialValue
    {
        public string VariableName { get; set; } = string.Empty;
        public short DataLength { get; set; }
        public string DataType { get; set; } = string.Empty;
        public string DataValue { get; set; } = string.Empty;

        public SpecialValue()
        {
        }

        public SpecialValue(string Data)
        {
            this.VariableName = Data.Substring(0, 19).Trim();
            this.DataType = Data.Substring(20, 2);
            this.DataLength = short.Parse(Data.Substring(22, 2));
            this.DataValue = Data.Substring(24, DataLength);
        }
    }
}