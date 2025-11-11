namespace OpenProtocol.Services
{
    using OpenProtocol.OpenProtocolTypes;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;


    public class OpenProtocolV2
    {
        private readonly TcpClientLayer _tcpClientLayer;

        private readonly System.Timers.Timer KeepAlivetimer = new System.Timers.Timer(8000);
        private int _lastTightId;

        public bool IsConnected => _tcpClientLayer.IsConnected;
        public event Action<ResultEvtArgs>? NewResultEvt;
        public event Action<EventArgs>? OnStationResult;
        public event Action<EventArgs>? OnBoltResult;
        public event Action<TracePlotEvtArgs>? OnTracePlotReceived; 
        public event Action<RelayStatusEvtArgs>? RelayStatusChanged;
        public event Action<UserDataEvtArgs>? UserDataReceived; 
        public event Action<LastTraceResultEvtArgs>? OnLastCurveReveived;
        public event Action<MutipleSpindlesResultsEvtArgs>? OnMultipleSpindleResultReceive;
        public event Action<ResultParameterSetId>? NewResultPmtId;  
        public event Action<JobEvtArgs>? NewJobEvt;
        public ControllerInformation? ControlerInf { get; private set; }
        public OpenProtocolV2()
        {
            _tcpClientLayer = new TcpClientLayer();
            _tcpClientLayer.MessageReceived += OnMessageReceived;
            _tcpClientLayer.ErrorOccurred += (ex) => Console.WriteLine($"Error: {ex.Message}");
            _tcpClientLayer.Disconnected += () => Console.WriteLine("Disconnected from server.");
            _tcpClientLayer.Connected += () => Console.WriteLine("Connected to server.");
            _tcpClientLayer.MessageSent += (message) => Console.WriteLine($"Message sent: {message}");

            KeepAlivetimer.Elapsed += async (sender, e) =>
            {
                await SendWithAckAsync(Mids.KeepAlive);
            };

        }

        public async Task connectAsync(string host, int port, List<Subscription> Subs)
        {
            try
            {
                  await _tcpClientLayer.ConnectAsync(host, port);
            }
            catch (Exception ex)
            {
                throw new Exception ($"Unable to connect to {host}:{port}", ex);
                
            }


            if (await SendWithAckAsync(Mids.CommunicationStart, 3,"", Mids.CommunicationStartAck))
            {
                Console.WriteLine("Communication started successfully.");
            }
            else
            {
                throw new Exception("Failed to start communication with the server.");
            }
            
            {
                foreach (var sub in Subs)
                {
                    if (!await SendWithAckAsync(sub.Type, sub.Rev))
                    {
                        throw new Exception($"Subscription to {sub.Type.ToString()} failed.");
                    }
                }              
                KeepAlivetimer.Start();
            }
        }
        private void OnMessageReceived(string message)
        {
            KeepAlivetimer.Stop();
            Console.WriteLine($"Message received: {message}");
            _ = AcknowledgeAsync(message);
            HandleMessage(message.Substring(4, 4), message, new List<short>());
            KeepAlivetimer.Start();
        }
        private async Task<bool> SendWithAckAsync<TEnum>(TEnum messageID, short rev=0, string data="", Mids expectedAck = Mids.CommandAcknowledgment)
        where TEnum : Enum
        {
            string builtMessage = OpMessage.Build(messageID, rev, data);
            return await _tcpClientLayer.SendAndWaitAckAsync(builtMessage,((short)expectedAck).ToString("D4"), 5000);
        }
        private async Task AcknowledgeAsync(string Message)
        {
            // Map of message IDs to their corresponding acknowledgment parameters
            var ackMap = new Dictionary<Mids, (Mids Ack, short rev, string data)>
            {
                [Mids.LastTighteningResult] = (Mids.LastTighteningResultAck, 0, ""),
                [Mids.JobInfoResult] = (Mids.JobInfoResultAck, 0, ""),
                // ["0035"] = ("0036", "000", ""),
                // ["0217"] = ("0218", "000", ""),
                // ["0101"] = ("0102", "000", ""),
                // ["0106"] = ("0108", "000", "1"),
                // ["0107"] = ("0108", "000", "1"),
                // ["0900"] = ("0005", "001", "0900"),,
                // ["0242"] = ("0243", "001", ""),
                // ["0212"] = ("0213", "001", ""),
                // ["0015"] = ("0016", "001", "")
            };

            Mids messageId = Enum.Parse<Mids>(Message.Substring(4, 4));
            // If message ID exists in map, send acknowledgment
            if (ackMap.TryGetValue(messageId, out var ackParams))
            {
                await _tcpClientLayer.SendAsync(OpMessage.Build(ackParams.Ack, ackParams.rev, ackParams.data));
            }
        }
        private void HandleMessage(string messageId, string message, List<short> data)
        {
            // Dictionary définissant les actions par messageId
            var messageHandlers = new Dictionary<string, Action<string, List<short>>>
            {

                ["0061"] = (msg, _) =>
                {
                    var currentResult = new ResultEvtArgs(msg);
                    if (currentResult.TightId != _lastTightId)
                    {
                        _lastTightId = currentResult.TightId;
                        NewResultEvt?.Invoke(currentResult);
                    }
                },
                ["0015"] = (msg, _) => NewResultPmtId?.Invoke( new ResultParameterSetId(msg)),
                ["0035"] = (msg, _) => NewJobEvt?.Invoke( new JobEvtArgs(msg)),
                ["0101"] = (msg, _) => OnMultipleSpindleResultReceive?.Invoke(new MutipleSpindlesResultsEvtArgs(msg)),
                ["0106"] = HandleStationResult,
                ["0107"] = HandleBoltResult,
                ["0900"] = (msg, d) => OnLastCurveReveived?.Invoke(new LastTraceResultEvtArgs(msg, d)),
                ["0901"] = (msg, _) => OnTracePlotReceived?.Invoke(new TracePlotEvtArgs(msg)),
                ["0211"] = (msg, _) => RelayStatusChanged?.Invoke( new RelayStatusEvtArgs(msg)),
                ["0242"] = (msg, _) => UserDataReceived?.Invoke(new UserDataEvtArgs(msg)),

            };

            if (messageHandlers.TryGetValue(messageId, out var handler))
            {
                handler(message, data);
            }
        }
        private void HandleStationResult(string message, List<short> _)
        {
            if (ControlerInf?.SysSubType is null) return;

            switch (ControlerInf.SysSubType)
            {
                case SystemSubTypeEnum.NotSet:
                case SystemSubTypeEnum.Normal:
                    OnStationResult?.Invoke( new LastTighteningResultStationDataEvtArg(message));
                    break;
                case SystemSubTypeEnum.Press:
                    OnStationResult?.Invoke( new LastPressResultStationDataEvtArg(message));
                    break;
            }
        }
        private void HandleBoltResult(string message, List<short> _)
        {
            if (ControlerInf?.SysSubType is null) return;

            switch (ControlerInf.SysSubType)
            {
                case SystemSubTypeEnum.Normal:
                    OnBoltResult?.Invoke(new LastTighteningResultBoltdataEvtArgs(message));
                    break;
                case SystemSubTypeEnum.Press:
                    OnBoltResult?.Invoke(new LastPressResultFittingdataEvtArgs(message));
                    break;
            }
        }


        
    }
}