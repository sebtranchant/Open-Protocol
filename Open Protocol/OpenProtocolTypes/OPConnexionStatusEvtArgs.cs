namespace OpenProtocol.OpenProtocolTypes
{
    public class OPConnexionStatusEvtArgs : EventArgs
    {
        private ConnexionStatusEnum _Status;

        public ConnexionStatusEnum Status
        {
            get { return _Status; }
            set { _Status = value; }
        }

        public OPConnexionStatusEvtArgs(ConnexionStatusEnum Status)
        {
            _Status = Status;
        }
    }

    public enum ConnexionStatusEnum
    {
        Disconnected,
        pending,
        Connected
    };
}