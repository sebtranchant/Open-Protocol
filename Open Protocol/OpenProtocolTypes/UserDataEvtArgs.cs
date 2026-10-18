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
            Message ??= string.Empty;
            if (Message.Length <= 20)
            {
                HexaValue = string.Empty;
                inputs = Array.Empty<byte>();
                return;
            }

            var hex = Message.Substring(20, Message.Length - 20);
            if (hex.Length % 2 != 0)
            {
                throw new FormatException("User data hex payload must have an even length.");
            }

            HexaValue = hex;
            inputs = Enumerable.Range(0, hex.Length / 2)
                .Select(x => Convert.ToByte(hex.Substring(x * 2, 2), 16))
                .ToArray();
        }
    }
}