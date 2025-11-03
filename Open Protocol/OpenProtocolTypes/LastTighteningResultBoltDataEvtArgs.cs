using OpenProtocol.OpenProtocolTypes;

namespace OpenProtocol
{
    public class LastTighteningResultBoltdataEvtArgs : EventArgs
    {
        private ResultBolt _resultbolt;

        public ResultBolt ResultBoltEvt
        {
            get { return _resultbolt; }
        }

        public LastTighteningResultBoltdataEvtArgs(string rawdata)
        {
            _resultbolt = new ResultBolt(rawdata);
        }
    }


    public class LastPressResultFittingdataEvtArgs : EventArgs
    {
        private ResultFitting _resultfitting;

        public ResultFitting ResultFittingEvt
        {
            get { return _resultfitting; }
        }

        public LastPressResultFittingdataEvtArgs(string rawdata)
        {
            _resultfitting = new ResultFitting(rawdata);
        }
    }
}