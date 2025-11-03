namespace OpenProtocol.Exceptions
{
    public class ProtocolException : Exception
    {
        public ProtocolException()
        {
        }

        public ProtocolException(string message)
            : base(String.Format("Protocol Error: {0}", message))
        {
        }
    }
}