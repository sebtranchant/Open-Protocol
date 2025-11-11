namespace OpenProtocol.OpenProtocolTypes
{
    public struct Subscription

    {
        public SubTypes Type;
        public short Rev;
    }

    public enum SubTypes
    {
        // Subscription messages
        SubscribeToResults = 60,    // MID 0010
        SubsribeToJobInfo = 34,    // MID 001
    }

}