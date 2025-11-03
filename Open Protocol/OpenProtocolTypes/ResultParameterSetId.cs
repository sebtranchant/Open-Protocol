using System.Globalization;

namespace OpenProtocol.OpenProtocolTypes
{
    public class ResultParameterSetId : EventArgs
    {
        private string _myEventText;


        public int id
        {
            get { return int.Parse(_myEventText.Substring(20,3)); }
        }

        public DateTime Date
        {
            get
            {
                DateTime date;
                CultureInfo provider = CultureInfo.InvariantCulture;
                var value = _myEventText.Substring(23, 17);
                string format = "yyyy-MM-dd:HH:mm:ss";
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

        public ResultParameterSetId(string ResultMessage)
        {
            _myEventText = ResultMessage;
        }
    }
}