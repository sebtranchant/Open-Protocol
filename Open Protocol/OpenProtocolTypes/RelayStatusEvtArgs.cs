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


            int i = 0;

            foreach (char item in rawdata)
            {
                switch (item)
                {
                    case '0':
                        relayStatus[i] = false;
                        break;
                    case '1':
                        relayStatus[i] = true;
                        break;
                    default:
                        break;
                }

                i++;
            }
        }
    }
}