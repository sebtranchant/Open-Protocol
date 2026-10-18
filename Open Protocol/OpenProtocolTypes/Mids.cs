namespace OpenProtocol.OpenProtocolTypes
{
    public enum Mids
    {
        CommunicationStart = 1,
        CommunicationStartAck = 2,
        CommunicationStop = 3,
        CommandError = 4,
        CommandAcknowledgment = 5,

        ParameterSetSelected = 15,
        ParameterSetSelectedAck = 16,

        JobInfoSubscribe = 34,
        JobInfoResult = 35,
        JobInfoResultAck = 36,

        EnableTool = 42,
        DisableTool = 43,

        LastTighteningSubscribe = 60,
        LastTighteningResult = 61,
        LastTighteningResultAck = 62,

        MultipleSpindleResult = 101,
        MultipleSpindleResultAck = 102,
        PowerMacsStationResult = 106,
        PowerMacsBoltResult = 107,
        PowerMacsResultAck = 108,

        StatusMonitoredInput = 200,
        RelayStatus = 211,
        RelayStatusAck = 213,
        AnalogInput = 217,
        AnalogInputAck = 218,
        UserData = 242,
        UserDataAck = 243,

        LastCurve = 900,
        LastTracePlot = 901,

        KeepAlive = 9999
    }
}
