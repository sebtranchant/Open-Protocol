namespace OpenProtocol.Services
{
    using OpenProtocol.Exceptions;
    using OpenProtocol.OpenProtocolTypes;

    public class OpenProtocolV2 : IDisposable
    {
        private static readonly string[] CommandAckMids = { "0005", "0004" };
        private static readonly string[] CommunicationStartAckMids = { "0002", "0004" };
        private static readonly string[] KeepAliveAckMids = { "9999" };

        private static readonly Dictionary<string, (Mids Ack, short Rev, string Data)> PushAcks = new()
        {
            ["0061"] = (Mids.LastTighteningResultAck, 0, ""),
            ["0035"] = (Mids.JobInfoResultAck, 0, ""),
            ["0217"] = (Mids.AnalogInputAck, 0, ""),
            ["0101"] = (Mids.MultipleSpindleResultAck, 0, ""),
            ["0106"] = (Mids.PowerMacsResultAck, 0, "1"),
            ["0107"] = (Mids.PowerMacsResultAck, 0, "1"),
            ["0900"] = (Mids.CommandAcknowledgment, 1, "0900"),
            ["0901"] = (Mids.CommandAcknowledgment, 1, "0901"),
            ["0242"] = (Mids.UserDataAck, 1, ""),
            ["0212"] = (Mids.RelayStatusAck, 1, ""),
            ["0015"] = (Mids.ParameterSetSelectedAck, 1, "")
        };

        private readonly TcpClientLayer _tcpClientLayer;
        private readonly System.Timers.Timer _keepAliveTimer = new(8000) { AutoReset = false };
        private int? _lastTightId;
        private int _disposed;

        public bool IsConnected => _tcpClientLayer.IsConnected;
        public ControllerInformation? ControlerInf { get; private set; }

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
        public event Action<IncomingMessage>? MessageReceived;
        public event Action<Exception>? ErrorOccurred;
        public event Action? Disconnected;
        public event Action? Connected;

        public OpenProtocolV2()
        {
            _tcpClientLayer = new TcpClientLayer();
            _tcpClientLayer.MessageReceived += OnMessageReceived;
            _tcpClientLayer.ErrorOccurred += ex => ErrorOccurred?.Invoke(ex);
            _tcpClientLayer.Disconnected += () =>
            {
                _keepAliveTimer.Stop();
                Disconnected?.Invoke();
            };
            _tcpClientLayer.Connected += () => Connected?.Invoke();

            _keepAliveTimer.Elapsed += async (_, _) => await OnKeepAliveElapsedAsync();
        }

        public async Task ConnectAsync(string host, int port, List<Subscription> subscriptions)
        {
            try
            {
                await _tcpClientLayer.ConnectAsync(host, port);
            }
            catch (Exception ex)
            {
                throw new ProtocolException($"Unable to connect to {host}:{port}", ex);
            }

            try
            {
                var startAck = await SendAndWaitAsync(Mids.CommunicationStart, 3, "", CommunicationStartAckMids);
                if (startAck.Mid == "0004")
                {
                    throw new ProtocolException("Communication start was refused (MID 0004).");
                }

                ControlerInf = new ControllerInformation(startAck.Ascii);

                foreach (var sub in subscriptions)
                {
                    var ack = await SendAndWaitAsync(sub.Type, sub.Rev, "", CommandAckMids);
                    if (ack.Mid == "0004")
                    {
                        throw new ProtocolException($"Subscription to {sub.Type} was refused (MID 0004).");
                    }
                }

                _keepAliveTimer.Start();
            }
            catch
            {
                try
                {
                    await DisconnectAsync();
                }
                catch
                {
                }

                throw;
            }
        }

        public async Task connectAsync(string host, int port, List<Subscription> subs)
        {
            await ConnectAsync(host, port, subs);
        }

        public async Task DisconnectAsync()
        {
            _keepAliveTimer.Stop();
            if (_tcpClientLayer.IsConnected)
            {
                try
                {
                    await _tcpClientLayer.SendAsync(OpMessage.Build(Mids.CommunicationStop, 0, ""));
                }
                catch
                {
                }
            }

            _tcpClientLayer.Disconnect();
        }

        private async Task OnKeepAliveElapsedAsync()
        {
            if (Volatile.Read(ref _disposed) != 0 || !_tcpClientLayer.IsConnected)
            {
                return;
            }

            try
            {
                await SendAndWaitAsync(Mids.KeepAlive, 0, "", KeepAliveAckMids, timeoutMs: 4000);
                if (_tcpClientLayer.IsConnected)
                {
                    _keepAliveTimer.Start();
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
                _tcpClientLayer.Disconnect();
            }
        }

        private void OnMessageReceived(IncomingMessage message)
        {
            _keepAliveTimer.Stop();
            try
            {
                MessageReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }

            try
            {
                HandleMessage(message);
                _ = AcknowledgeAsync(message.Mid);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
            finally
            {
                if (_tcpClientLayer.IsConnected)
                {
                    _keepAliveTimer.Start();
                }
            }
        }

        private async Task<IncomingMessage> SendAndWaitAsync<TEnum>(
            TEnum messageId,
            short rev,
            string data,
            IReadOnlyCollection<string> expectedAckMids,
            int timeoutMs = 5000)
            where TEnum : Enum
        {
            var built = OpMessage.Build(messageId, rev, data);
            return await _tcpClientLayer.SendAndWaitAckAsync(built, expectedAckMids, timeoutMs);
        }

        private async Task AcknowledgeAsync(string messageId)
        {
            if (!PushAcks.TryGetValue(messageId, out var ackParams))
            {
                return;
            }

            try
            {
                await _tcpClientLayer.SendAsync(OpMessage.Build(ackParams.Ack, ackParams.Rev, ackParams.Data));
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
        }

        private void HandleMessage(IncomingMessage incoming)
        {
            switch (incoming.Mid)
            {
                case "0002":
                    ControlerInf = new ControllerInformation(incoming.Ascii);
                    break;
                case "0061":
                    var currentResult = new ResultEvtArgs(incoming.Ascii);
                    if (currentResult.TightId != _lastTightId)
                    {
                        _lastTightId = currentResult.TightId;
                        NewResultEvt?.Invoke(currentResult);
                    }
                    break;
                case "0015":
                    NewResultPmtId?.Invoke(new ResultParameterSetId(incoming.Ascii));
                    break;
                case "0035":
                    NewJobEvt?.Invoke(new JobEvtArgs(incoming.Ascii));
                    break;
                case "0101":
                    OnMultipleSpindleResultReceive?.Invoke(new MutipleSpindlesResultsEvtArgs(incoming.Ascii));
                    break;
                case "0106":
                    HandleStationResult(incoming.Ascii);
                    break;
                case "0107":
                    HandleBoltResult(incoming.Ascii);
                    break;
                case "0900":
                    OnLastCurveReveived?.Invoke(new LastTraceResultEvtArgs(incoming.Ascii, incoming.CurveSamples.ToList()));
                    break;
                case "0901":
                    OnTracePlotReceived?.Invoke(new TracePlotEvtArgs(incoming.Ascii));
                    break;
                case "0211":
                    RelayStatusChanged?.Invoke(new RelayStatusEvtArgs(incoming.Ascii));
                    break;
                case "0242":
                    UserDataReceived?.Invoke(new UserDataEvtArgs(incoming.Ascii));
                    break;
            }
        }

        private void HandleStationResult(string message)
        {
            switch (ControlerInf?.SysSubType ?? SystemSubTypeEnum.NotSet)
            {
                case SystemSubTypeEnum.Press:
                    OnStationResult?.Invoke(new LastPressResultStationDataEvtArg(message));
                    break;
                default:
                    OnStationResult?.Invoke(new LastTighteningResultStationDataEvtArg(message));
                    break;
            }
        }

        private void HandleBoltResult(string message)
        {
            switch (ControlerInf?.SysSubType ?? SystemSubTypeEnum.Normal)
            {
                case SystemSubTypeEnum.Press:
                    OnBoltResult?.Invoke(new LastPressResultFittingdataEvtArgs(message));
                    break;
                default:
                    OnBoltResult?.Invoke(new LastTighteningResultBoltdataEvtArgs(message));
                    break;
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            _keepAliveTimer.Stop();
            _keepAliveTimer.Dispose();
            _tcpClientLayer.Dispose();
        }
    }
}
