namespace OpenProtocol.OpenProtocolTypes
{
    public enum Mids
    {
        // Communication
        CommunicationStart = 1,     // MID 0001
        CommunicationStartAck = 2,     // MID 0002

        // Command acknowledgments
        CommandAcknowledgment = 5,    // MID 0005   
        CommandError = 4,    // MID 0004

        // Commands
        EnableTool = 042,    // MID 0042
        DisableTool = 043,   // MID 0043

       
        // Job Info Results
        JobInfoResult = 35,    // MID 0035
        JobInfoResultAck = 36,    // MID 0036

        // Tightening results
        LastTighteningResult = 61,    // MID 0060
        LastTighteningResultAck = 62,    // MID 0061

        // Curve / Trace
        LastCurve = 70,    // MID 0070
        LastCurveAck = 71,    // MID 0071

        // Job info
        JobInfoRequest = 120,   // MID 0120
        JobInfoAck = 121,   // MID 0121

        // Status
        StatusMonitoredInput = 200,   // MID 0200
        StatusMonitoredInputAck = 201,   // MID 0201

        // Keep alive
        KeepAlive = 9999,  // MID 9999
        KeepAliveAck = 9999,   // MID 9998

        
    }
}