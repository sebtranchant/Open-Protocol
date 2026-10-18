namespace OpenProtocol.Exceptions
{
    public class ProtocolException : Exception
    {
        public ProtocolException()
        {
        }

        public ProtocolException(string message)
            : base(string.Format("Protocol Error: {0}", message))
        {
        }

        public ProtocolException(string message, Exception inner)
            : base(string.Format("Protocol Error: {0}", message), inner)
        {
        }
    }
}