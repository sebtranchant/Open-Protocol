namespace OpenProtocol.OpenProtocolTypes
{
    public class TracePlotEvtArgs : EventArgs
    {
        TracePlot _TrcPlot;

        public TracePlot TracerPlot
        {
            get { return _TrcPlot; }
        }


        public TracePlotEvtArgs(string Message)
        {
            _TrcPlot = new TracePlot(Message);
        }
    }
}