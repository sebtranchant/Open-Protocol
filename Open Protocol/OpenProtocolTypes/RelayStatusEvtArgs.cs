namespace OpenProtocol.OpenProtocolTypes
{
    public class RelayStatusEvtArgs : EventArgs
    {
        private bool[] relayStatus;

        public bool[] RelayStatustEvt
        {
            get { return relayStatus; }
        }


        public RelayStatusEvtArgs(string rawdata)
        {
            relayStatus = new bool[8];
            var payload = rawdata.Length > 20 ? rawdata.Substring(20) : rawdata;
            var i = 0;
            foreach (var item in payload)
            {
                if (i >= relayStatus.Length)
                {
                    break;
                }

                if (item == '0')
                {
                    relayStatus[i++] = false;
                }
                else if (item == '1')
                {
                    relayStatus[i++] = true;
                }
            }
        }
    }
}