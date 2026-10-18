namespace OpenProtocol.OpenProtocolTypes
{
    public struct Subscription
    {
        public SubTypes Type;
        public short Rev;
    }

    public enum SubTypes
    {
        SubscribeToResults = 60,
        SubsribeToJobInfo = 34
    }
}
