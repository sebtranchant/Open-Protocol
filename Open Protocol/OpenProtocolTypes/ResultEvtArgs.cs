using System.Globalization;

namespace OpenProtocol.OpenProtocolTypes
{
    public class ResultEvtArgs : EventArgs
    {
        private string _myEventText;


        public string Mid
        {
            get { return _myEventText.Substring(4, 4); }
        }

        public string data
        {
            get { return _myEventText.Substring(20); }
        }

        public int TightId
        {
            get { return int.Parse(_myEventText.Substring(304, 9)); }
        }

        public DateTime Date
        {
            get
            {
                DateTime date;
                CultureInfo provider = CultureInfo.InvariantCulture;
                var value = _myEventText.Substring(345, 19);
                string format = "yyyy-MM-dd:hh:mm:ss";
                try
                {
                    date = DateTime.ParseExact(value, format, provider);
                }
                catch (Exception)
                {
                    return DateTime.Now;
                }


                return date;
            }
        }

        public float Couple
        {
            get { return float.Parse(_myEventText.Substring(183, 6)) / 100; }
        }

        public float CoupleMin
        {
            get { return float.Parse(_myEventText.Substring(159, 6)) / 100; }
        }

        public float CoupleCible
        {
            get { return float.Parse(_myEventText.Substring(175, 6)) / 100; }
        }

        public float CoupleMax
        {
            get { return float.Parse(_myEventText.Substring(167, 6)) / 100; }
        }

        public float PrevailTorque
        {
            get
            {
                try
                {
                    return float.Parse(_myEventText.Substring(295, 6)) / 100;
                }
                catch (Exception)
                {

                    return 0;
                }
            }
        }

        public ResultPFValueSatus CoupleStatus
        {
            get { return (ResultPFValueSatus)(int.Parse(_myEventText.Substring(126, 1))); }
        }

        public int Angle
        {
            get { return int.Parse(_myEventText.Substring(212, 5)); }
        }

        public int AngleMin
        {
            get { return int.Parse(_myEventText.Substring(191, 5)); }
        }

        public int AngleCible
        {
            get { return int.Parse(_myEventText.Substring(205, 5)); }
        }

        public int AngleMax
        {
            get { return int.Parse(_myEventText.Substring(198, 5)); }
        }

        public ResultPFValueSatus AngleStatus
        {
            get { return (ResultPFValueSatus)(int.Parse(_myEventText.Substring(129, 1))); }
        }

        public string Status
        {
            get 
            { 
                switch (_myEventText.Substring(120, 1))
                {
                    case "0":
                        return "NOK";
                    case "1":
                        return "OK";

                    default:
                        return "N/A";
                }
                
            }
        }

        public int Bacthsize
        {
            get { return int.Parse(_myEventText.Substring(109, 3)); }
        }

        public int Counter
        {
            get { return int.Parse(_myEventText.Substring(115, 3)); }
        }

        public string BatchStatus
        {
            get
            {
                switch (_myEventText.Substring(219, 1))
                {
                    case "0":
                        return "NOK";
                    case "1":
                        return "OK";

                    default:
                        return "N/A";
                }
            }
        }

        public string TCName
        {
            get { return _myEventText.Substring(32, 25).Trim(); }
        }

        public int ChannelID
        {
            get { return int.Parse(_myEventText.Substring(28, 2)); }
        }

        public int JobID
        {
            get { return int.Parse(_myEventText.Substring(86, 4)); }
        }

        public string ToolSerial
        {
            get { return _myEventText.Substring(329, 14).Trim(); }
        }

        public string PsetName
        {
            get { return _myEventText.Substring(387, 25).Trim(); }
        }

        public string Indentifier1
        {
            get { return _myEventText.Substring(59, 25).Trim(); }
        }

        public string Indentifier2
        {
            get { return _myEventText.Substring(421, 25).Trim(); }
        }

        public string Indentifier3
        {
            get { return _myEventText.Substring(448, 25).Trim(); }
        }

        public ResultType RsltType
        {
            get { return (ResultType)(int.Parse(_myEventText.Substring(417, 2))); }
        }

        public ResultEvtArgs(string ResultMessage)
        {
            _myEventText = ResultMessage;
        }
    }

    public enum ResultType
    {
        Tightening = 1,
        Loosening = 2,
        BatchIncrement = 3,
        BatchDecrement = 4,
        Bypass = 5,
        Abort = 6,
        SyncTightening = 7,
        Reference = 8
    };

    public enum ResultPFTighteningStatus
    {
        NOk = 0,
        Ok = 1
    }

    public enum ResultPFValueSatus
    {
        Low = 0,
        OK = 1,
        High = 2
    }
}