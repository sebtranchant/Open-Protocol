namespace OpenProtocol.OpenProtocolTypes
{
    public class UserDataEvtArgs : EventArgs
    {
        private Byte[] inputs;

        public Byte[] Inputs
        {
            get { return inputs; }
            set { inputs = value; }
        }

        public string HexaValue { get; set; }

        public UserDataEvtArgs(string Message)
        {
            string Hex = Message.Substring(20, Message.Length - 21);
            HexaValue = Hex;
            inputs = Enumerable.Range(0, Hex.Length / 2).Select(x => Convert.ToByte(Hex.Substring(x * 2, 2), 16))
                .ToArray();
        }
    }
}