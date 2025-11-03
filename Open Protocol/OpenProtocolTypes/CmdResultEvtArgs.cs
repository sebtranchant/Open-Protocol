namespace OpenProtocol
{
    public class CmdResultEvtArgs : EventArgs
    {
        public CmdResult CommandResult { get; set; }
        public short Mid { get; set; }
        public short ErrorCode { get; set; }

        public CmdResultEvtArgs(CmdResult result, short mid, short errorcode)
        {
            CommandResult = result;
            Mid = mid;
            ErrorCode = errorcode;
        }
    }

    public enum CmdResult
    {
        Success,
        Error
    }
}