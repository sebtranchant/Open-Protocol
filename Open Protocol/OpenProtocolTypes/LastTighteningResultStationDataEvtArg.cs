namespace OpenProtocol.OpenProtocolTypes
{
    public class LastTighteningResultStationDataEvtArg : EventArgs
    {
        private TightResultStation _resultstation;

        public TightResultStation ResultStationEvt
        {
            get { return _resultstation; }
        }

        public LastTighteningResultStationDataEvtArg(string rawdata)
        {
            _resultstation = new TightResultStation(rawdata);
        }
    }

    public class LastPressResultStationDataEvtArg : EventArgs
    {
        private PressResultStation _resultstation;

        public PressResultStation ResultStationEvt
        {
            get { return _resultstation; }
        }

        public LastPressResultStationDataEvtArg(string rawdata)
        {
            _resultstation = new PressResultStation(rawdata);
        }
    }
}