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

        public DateTime? Date
        {
            get
            {
                try
                {
                    var value = _myEventText.Substring(23, 19);
                    return DateTime.ParseExact(value, "yyyy-MM-dd:HH:mm:ss", CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public ResultParameterSetId(string ResultMessage)
        {
            _myEventText = ResultMessage;
        }
    }
}