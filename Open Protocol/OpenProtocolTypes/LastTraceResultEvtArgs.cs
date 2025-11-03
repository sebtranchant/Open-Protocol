namespace OpenProtocol.OpenProtocolTypes
{
    public class LastTraceResultEvtArgs : EventArgs
    {
        TraceResult _Traceresult;

        public TraceResult Tracerslt
        {
            get { return _Traceresult; }
        }


        public LastTraceResultEvtArgs(string Message, List<short> CurveData)
        {
            _Traceresult = new TraceResult(Message, CurveData);
        }
    }
}