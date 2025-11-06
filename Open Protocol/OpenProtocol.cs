using Diziscop.AtlasCopcoContribution.Nutrunner.OpenProtocol.OpenProtocolTypes;
using OpenProtocol.Exceptions;
using OpenProtocol.OpenProtocolTypes;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Timers;


namespace OpenProtocol
{
    public class OPClient
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(OPClient));

        #region Fields

        private static ManualResetEvent CommandAck = new ManualResetEvent(false);
        private readonly int _port;
        private bool ClosePending = false;
        private ToolInformations? _ToolInformations;
        private TcpClient? Myclient = null;
        private StreamReader? StrIN = null;
        private StreamWriter? StrOUT = null;
        private readonly List<Subscriptions> _Subscriptions = new();
        private readonly System.Timers.Timer KeepAlivetimer = new System.Timers.Timer(8000);
        private readonly System.Timers.Timer TimeOut = new System.Timers.Timer(4000); //  Reception Time out before disconnection
        private readonly System.Timers.Timer ConnectionWatchDog = new System.Timers.Timer(5000);
        private const char nul = (char)00;
        private StateObject? _stateObj;
        private int _lastTightId = 0;
        private string LastUserMessage = "";
        private Marques _marque;
        #endregion

        #region Properties
        public Task<Hashtable>? PsetsnamePerJob;
        public Task<List<ProgDescription>>? ProgDescList;
        public event EventHandler<ResultEvtArgs>? NewResultEvt;
        public event EventHandler<ResultParameterSetId>? NewResultPmtId;
        public event EventHandler<JobEvtArgs>? NewJobEvt;
        public event EventHandler<MutipleSpindlesResultsEvtArgs>? OnMultipleSpindleResultReceive;
        public event EventHandler<EventArgs>? OnStationResult;
        public event EventHandler<EventArgs>? OnBoltResult;
        public event EventHandler<OPConnexionStatusEvtArgs>? ConnexionStatusChanged;
        public event EventHandler<LastTraceResultEvtArgs>? OnLastCurveReveived;
        public event EventHandler<TracePlotEvtArgs>? OnTracePlotReceived;
        public event EventHandler<RelayStatusEvtArgs>? RelayStatusChanged;
        public event EventHandler<UserDataEvtArgs>? UserDataReceived;
        public event EventHandler<CmdResultEvtArgs>? CommandResult;

        public string IpAddress { get; } = string.Empty;

        public ControllerInformation? ControlerInf { get; private set; }

        public ToolInformations? ToolInfos => _ToolInformations;

        public bool Initok { get; } = false;

        public ConnexionStatusEnum CurrentCxStatus { get; private set; }
        public bool AutoReconnect { get; set; }





        #endregion

        #region Methodes privées

        private void KeepAlivetimer_Elapsed(object? sender, ElapsedEventArgs e)
        {

            TimeOut.Start();
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            SendAsync("0020", "9999", "000", "");
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
        }

        private void TimeOut_Elapsed(object? sender, ElapsedEventArgs e)
        {
            log4net.ThreadContext.Properties["myContext"] = this.IpAddress;
            log.Error("Time Out on Keep Alive");
            this.UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);
        }

        private async Task GenericDataSusbscription(int SubscriptionMID, int Revision, string ExtraData)
        {
            //TODO add ack like SendAsync
            if (StrOUT == null) return;
            int GenericRev = 1;
            string Message = "000800" + (int)GenericRev + "         " + SubscriptionMID.ToString().PadLeft(4, '0') +
                             Revision.ToString().PadLeft(3, '0') + ExtraData.Length.ToString().PadLeft(2, '0') +
                             ExtraData;
            ;
            try
            {
                Message = (Message.Length + 4).ToString().PadLeft(4, '0') + Message + nul;
                await StrOUT.WriteAsync(Message);
            }
            catch (Exception ex)
            {
                KeepAlivetimer.Stop();
                log.Error("Erreur GenericDataSusbscription() " + IpAddress + "  " + ex.Message);
                this.UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);
            }
        }
        private Task SendAsync(string length, String Mid, string Rev, String data)
        {
            return SendAsync(length, Mid, Rev, false, data);
        }
        private async Task SendAsync(string length, String Mid, string Rev, bool Ack, String data)
        {
            if (StrOUT == null) return;
            string Message = "";
            if (Ack)
            {
                log4net.ThreadContext.Properties["myContext"] = this.IpAddress;
                Message = length + Mid + Rev + "         " + data + nul;
            }
            else
            {
                log4net.ThreadContext.Properties["myContext"] = this.IpAddress;
                Message = length + Mid + Rev + '1' + "        " + data + nul;
            }


            try
            {
                await StrOUT.WriteAsync(Message);
            }
            catch (Exception ex)
            {
                KeepAlivetimer.Stop();
                log.Error("Erreur Send() " + IpAddress + "  " + ex.Message);
                this.UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);
                throw new ProtocolException(ex.Message);
            }

            //if (Mid != "9999") // No log on Keep Alive
            //{
            log.Debug("PC-->CT : " + Message);
            //}
        }

        private async Task<string> ReceiveAsync()
        {
            if (Myclient == null) return String.Empty;
            if (StrIN == null) return String.Empty;

            bool exit = false;
            char[]? chararray = null;
            try
            {
                do
                {
                    if (Myclient.Available != 0)
                    {
                        chararray = new char[Myclient.Available];
                        await StrIN.ReadAsync(chararray, 0, chararray.Length);
                        exit = true;
                    }
                } while (exit == false);
            }
            catch (Exception ex)
            {
                KeepAlivetimer.Stop();
                log.Error("Erreur ASyncReceive() " + IpAddress + "  " + ex.Message);
                this.UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);
            }

            string reponse = new string(chararray);
            log.Debug("PC<--CT : " + reponse);
            return reponse;
        }

        private void OnDataReceived(IAsyncResult asyn)
        {
            log4net.ThreadContext.Properties["myContext"] = this.IpAddress;
            _stateObj = (StateObject?)asyn.AsyncState;
            if (_stateObj != null)
            {
                if (_stateObj.workSocket != null)
                {
                    try
                    {
                        _stateObj.RxBytesCount = _stateObj.workSocket.EndReceive(asyn);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Erreur OnDataReceived() EndReceive " + ex.Message);
                        this.UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);

                    }
                }
                else
                {
                    _stateObj.RxBytesCount = 0;
                }

                if (_stateObj.RxBytesCount > 0)
                {
                    _stateObj.BytesMessage.AddRange(_stateObj.buffer.Take(_stateObj.RxBytesCount));
                }
                else
                {
                    _stateObj.BytesMessage.Clear();
                    _stateObj.PartialMessage = false;
                    return;
                }

                // get message length in the firts 4 chars

                if (!_stateObj.PartialMessage)
                {
                    _stateObj.FullMsgLen = int.Parse(Encoding.ASCII.GetString(_stateObj.buffer, 0, 4));
                }

                if ((_stateObj.FullMsgLen == _stateObj.BytesMessage.Count - 1) && !_stateObj.PartialMessage || _stateObj.FullMsgLen == _stateObj.BytesMessage.Count)
                {
                    _stateObj.PartialMessage = false;

                    //  log.Info("Message byte count :" + (_stateObj.BytesMessage.Count) + "/" + _stateObj.FullMsgLen + " Partial message : " + _stateObj.PartialMessage);
                    this.DecodeMessage(_stateObj.BytesMessage);
                    _stateObj.BytesMessage.Clear();
                }
                else if
                    (_stateObj.FullMsgLen < _stateObj.BytesMessage.Count)
                {
                    _stateObj.PartialMessage = false;

                    //  log.Info("Message byte count :" + (_stateObj.BytesMessage.Count) + "/" + _stateObj.FullMsgLen + " Partial message : " + _stateObj.PartialMessage);
                    this.DecodeMessage(_stateObj.BytesMessage);
                    _stateObj.BytesMessage.Clear();


                }
                else
                {
                    _stateObj.PartialMessage = true;
                    log.Info("Message partial count :" + (_stateObj.BytesMessage.Count - 1) + "/" + _stateObj.FullMsgLen + " Partial message : " + _stateObj.PartialMessage);
                    this.Ecoute();
                }
            }
        }

        private void DecodeMessage(List<byte> ByteToDecode)
        {

            List<short> Data = new List<short>();

            int endindex = ByteToDecode.IndexOf(0);
            string opmessage = Encoding.ASCII.GetString(ByteToDecode.ToArray(), 0, endindex);
            if (opmessage.Substring(4, 4) == "0900")
            {
                int samplecount = int.Parse(opmessage.Substring(opmessage.Length - 5, 5));
                int index = 0;
                try
                {


                    if (samplecount > 0) // données présente aprés la trame ASCII
                    {
                        byte[] Tracebytes = new byte[samplecount * 2];

                       // var test = ByteToDecode.ToArray();
                        Buffer.BlockCopy(ByteToDecode.ToArray(), opmessage.Length + 1, Tracebytes, 0, samplecount * 2);
                        Data = Tracebytes.GroupBy(x => index++ / 2)
                          .Select(x => BitConverter.ToInt16(x.Reverse().ToArray(), 0)).ToList();
                        log.Info("Sample count :  " + Data.Count + "Expected :" + samplecount);
                    }

                }
                catch (ArgumentOutOfRangeException ex)
                {
                    log.Error("Erreur OnDataReceived() Blockcopy " + IpAddress + "/buffer :" + _stateObj?.buffer + "/EndIndex :" + _stateObj?.EndIndex.ToString() + "/" + ex.ToString());
                    _stateObj?.BytesMessage.Clear();
                    this.UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);
                }
                catch (Exception ex)
                {
                    log.Error("Erreur OnDataReceived() decodage data " + IpAddress + "/irx :" + _stateObj?.RxBytesCount.ToString() + "/EndIndex :" + _stateObj?.EndIndex.ToString() + ex.Message);
                    _stateObj?.BytesMessage.Clear();
                    this.UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);
                }
            }
            _stateObj?.BytesMessage.Clear();
            this.Compute(opmessage, Data);
        }
        private void Compute(string message, List<short> data)
        {
            log4net.ThreadContext.Properties["myContext"] = IpAddress;
            KeepAlivetimer.Stop();
            
            if (string.IsNullOrEmpty(message) || message.Length <= 8)
            {
                log.Debug($"Message malformée reçue : {message}");
                UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);
                return;
            }

            var messageId = message[4..8];
            
            if (messageId != "9999") 
            {
                log.Debug($"PC<--CT : {message}");
            }
    
            _ = AcknowledgeAsync(messageId);

            try
            {
                HandleMessage(messageId, message, data);
            }
            catch (Exception ex)
            {
                log.Error($"Erreur lors du traitement du message {messageId}: {ex.Message}");
                UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);
                return;
            }

    if (!ClosePending)
    {
        KeepAlivetimer.Start();
        Ecoute();
    }
}

private void HandleMessage(string messageId, string message, List<short> data)
{
    // Dictionary définissant les actions par messageId
    var messageHandlers = new Dictionary<string, Action<string, List<short>>>
    {
        ["9999"] = (_,_) => 
        {
            KeepAlivetimer.Stop();
            TimeOut.Stop();
        },
        ["0061"] = (msg,_) =>
        {
            var currentResult = new ResultEvtArgs(msg);
            if (currentResult.TightId != _lastTightId)
            {
                _lastTightId = currentResult.TightId;
                NewResultEvt?.Invoke(this, currentResult);
            }
        },
        ["0015"] = (msg,_) => NewResultPmtId?.Invoke(this, new ResultParameterSetId(msg)),
        ["0035"] = (msg,_) => NewJobEvt?.Invoke(this, new JobEvtArgs(msg)),
        ["0101"] = (msg,_) => OnMultipleSpindleResultReceive?.Invoke(this, new MutipleSpindlesResultsEvtArgs(msg)),
        ["0106"] = HandleStationResult,
        ["0107"] = HandleBoltResult,
        ["0900"] = (msg,d) => OnLastCurveReveived?.Invoke(this, new LastTraceResultEvtArgs(msg, d)),
        ["0901"] = (msg,_) => OnTracePlotReceived?.Invoke(this, new TracePlotEvtArgs(msg)),
        ["0211"] = (msg,_) => RelayStatusChanged?.Invoke(this, new RelayStatusEvtArgs(msg)),
        ["0242"] = (msg,_) => UserDataReceived?.Invoke(this, new UserDataEvtArgs(msg)),
        ["0004"] = HandleCommandRefused,
        ["0005"] = HandleCommandAccepted,
        ["0002"] = HandleCommandAccepted
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
            OnStationResult?.Invoke(this, new LastTighteningResultStationDataEvtArg(message));
            break;
        case SystemSubTypeEnum.Press:
            OnStationResult?.Invoke(this, new LastPressResultStationDataEvtArg(message));
            break;
    }
}

private void HandleBoltResult(string message, List<short> _)
{
    if (ControlerInf?.SysSubType is null) return;

    switch (ControlerInf.SysSubType)
    {
        case SystemSubTypeEnum.Normal:
            OnBoltResult?.Invoke(this, new LastTighteningResultBoltdataEvtArgs(message));
            break;
        case SystemSubTypeEnum.Press:
            OnBoltResult?.Invoke(this, new LastPressResultFittingdataEvtArgs(message));
            break;
    }
}

private void HandleCommandRefused(string message, List<short> _)
{
    var cmdId = message[20..26];
    log.Info($"PC-->PF : Commande refusée : {cmdId}");
    CommandResult?.Invoke(this, new CmdResultEvtArgs(CmdResult.Error, short.Parse(cmdId), 0));
    CommandAck.Set();
}

private void HandleCommandAccepted(string message, List<short> _)
{
    var cmdId = message[20..24];
    log.Info($"PC-->PF : Commande acceptée : {cmdId}");
    CommandAck.Set();
    CommandResult?.Invoke(this, new CmdResultEvtArgs(CmdResult.Success, short.Parse(cmdId), 0));
}
        private async Task AcknowledgeAsync(string MessageId)
        {
            // Map of message IDs to their corresponding acknowledgment parameters
            var ackMap = new Dictionary<string, (string length, string mid, string rev, string data)>
            {
                ["0061"] = ("0020", "0062", "000", ""),
                ["0035"] = ("0020", "0036", "000", ""),
                ["0217"] = ("0020", "0218", "000", ""),
                ["0101"] = ("0020", "0102", "000", ""),
                ["0106"] = ("0021", "0108", "000", "1"),
                ["0107"] = ("0021", "0108", "000", "1"),
                ["0900"] = ("0024", "0005", "001", "0900"),
                ["0901"] = ("0024", "0005", "001", "0901"),
                ["0242"] = ("0020", "0243", "001", ""),
                ["0212"] = ("0020", "0213", "001", ""),
                ["0015"] = ("0020", "0016", "001", "")
            };

            // If message ID exists in map, send acknowledgment
            if (ackMap.TryGetValue(MessageId, out var ackParams))
            {
                await SendAsync(ackParams.length, ackParams.mid, ackParams.rev, ackParams.data)
                    .ConfigureAwait(false);
            }
        }

        private void UpdateConnectionStatus(ConnexionStatusEnum status)
        {
            if (CurrentCxStatus != status)
            {
                CurrentCxStatus = status;
                ConnexionStatusChanged?.Invoke(this, new OPConnexionStatusEvtArgs(CurrentCxStatus));
                if (ControlerInf != null)
                {
                    log.Info(
                        $"Connexion status for {ControlerInf.ControllerName} Updated to {CurrentCxStatus.ToString()}");
                }
                else
                {
                    log.Info($"Connexion status for {IpAddress} Updated to {CurrentCxStatus.ToString()}");
                }
            }
        }

        private async Task LastResultSubscribe()
        {
            if (Myclient == null) return;

            if (Myclient.Connected == true)
            {
                log.Info("PC-->PF : Souscription Resultat");
                await SendAsync("0020", "0060", "004", "");
            }
        }

        private async Task ConfigurationEquipmentSubscribe()
        {
            if (Myclient == null) return;


            if (Myclient.Connected == true)
            {
                log.Info("PC-->PF : Souscription Configuration");
                await SendAsync("0020", "0014", "001", "");
            }
        }

        private async Task JobSubscribe()
        {
            if (Myclient == null) return;

            if (Myclient.Connected == true)
            {
                log.Info("PC-->PF : Souscription Info Job");
                await SendAsync("0020", "0034", "002", "");
            }
        }

        private async Task MultipleSpindleResultSubscibeTest()
        {
            if (Myclient == null) return;

            if (Myclient.Connected == true)
            {
                log.Info("PC-->PF : Souscription  MultipleSpindleResult");
                await SendAsync("0020", "0100", "001", "");
            }
        }

        private async Task SubscribeLastPowerMACSTighteningResultTest()
        {
            if (Myclient == null) return;

            if (Myclient.Connected == true)
            {
                log.Info("PC-->PF : Souscription SubscribeLastPowerMACSTighteninResult");
                await SendAsync("0031", "0105", "004", "00000000001");
            }
        }

        private async Task SubscribeMonitoredInput()
        {
            if (Myclient == null) return;

            if (Myclient.Connected == true)
            {
                log.Info("PC-->PF : Souscription  SubscribeMonitoredInput");
                await SendAsync("0020", "0210", "001", "");
            }
        }

        private async Task SubscibeUSerDataMessage()
        {
            if (Myclient == null) return;

            if (Myclient.Connected == true)
            {
                log.Info("PC-->CT : Souscription  SubscribeUSerDataMessage");
                await SendAsync("0020", "0241", "001", "");
            }
        }

        private async Task SubscribeLastCurve()
        {
            if (Myclient == null) return;

            if (Myclient.Connected == true)
            {
                switch (ControlerInf!.SysSubType)
                {
                    case SystemSubTypeEnum.Normal:
                        log.Info("PC-->PF : Souscription SubscribeLastCurve Normal System");
                        await GenericDataSusbscription(900, 1, "00000000000000000000000000000002001002");
                        break;
                    case SystemSubTypeEnum.Press:
                        log.Info("PC-->PF : Souscription SubscribeLastCurve Press System");
                        await GenericDataSusbscription(900, 1, "00000000000000000000000000000002005006");
                        break;
                    default:
                        log.Info("PC-->PF : Souscription SubscribeLastCurve Normal System");
                        await GenericDataSusbscription(900, 1, "00000000000000000000000000000002001002");
                        break;
                }
            }
        }

        private async Task SubscribeTracePlot()
        {
            if (Myclient == null) return;

            if (Myclient.Connected == true)
            {
                log.Info("PC-->PF : Souscription SubscribeLastPowerMACSTracePlot");
                await GenericDataSusbscription(901, 1, "");
            }
        }

        private void ConnectionWatchDog_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (CurrentCxStatus == ConnexionStatusEnum.Disconnected)
            {
                ConnectionWatchDog.Stop();
                log.Debug("Détection deconnexion par le WatchDog");
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                ConnectAsync();
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            }
            else

            {
                log.Debug("WatchDog is watching");

                ConnectionWatchDog.Start();
            }
        }

        #endregion

        #region Methodes publiques

        public OPClient(string IP, int port, bool log)
        {
            IpAddress = IP;
            _port = port;
            KeepAlivetimer.Elapsed += new ElapsedEventHandler(KeepAlivetimer_Elapsed);
            Initok = true;
        }

        public OPClient(string IP, int port, List<Subscriptions> Subscriptions)
        {
            IpAddress = IP;
            _port = port;
            _Subscriptions = Subscriptions;
            KeepAlivetimer.Elapsed += new ElapsedEventHandler(KeepAlivetimer_Elapsed);
            KeepAlivetimer.AutoReset = false;
            TimeOut.Elapsed += TimeOut_Elapsed;
            TimeOut.AutoReset = false;
        }

        public OPClient(string IP, int port, List<Subscriptions> Subscriptions, bool AutoReconnection, Marques marque)

        {
            IpAddress = IP;
            _port = port;
            _Subscriptions = Subscriptions;
            _marque = marque;
            KeepAlivetimer.Elapsed += new ElapsedEventHandler(KeepAlivetimer_Elapsed);
            KeepAlivetimer.AutoReset = false;
            TimeOut.Elapsed += TimeOut_Elapsed;
            TimeOut.AutoReset = false;
            if (AutoReconnection)
            {
                ConnectionWatchDog.Elapsed += ConnectionWatchDog_Elapsed;
                ConnectionWatchDog.AutoReset = false;
            }

            AutoReconnect = AutoReconnection;
            log4net.ThreadContext.Properties["myContext"] = IpAddress;
        }

        public async Task ConnectAsync()
        {
            log4net.ThreadContext.Properties["myContext"] = this.IpAddress;

            log.Info("************** DEBUT CONNEXION avec " + IpAddress + " **********************");
            LastUserMessage = "";
            UpdateConnectionStatus(ConnexionStatusEnum.pending);
            if (AutoReconnect)
            {
                ConnectionWatchDog.Start();
                log.Debug("Watchdog connection démmaré");
            }

            #region TCPClient Connection

            log.Debug("Ouverture port TCP avec " + IpAddress);
            try
            {
                if (Myclient != null)
                {
                    Myclient.Close();
                }

                Myclient = new TcpClient
                {
                    SendTimeout = 1000,
                    ReceiveTimeout = 1000
                };


                await Myclient.ConnectAsync(IpAddress, _port);

                StrIN = new StreamReader(Myclient.GetStream());
                StrOUT = new StreamWriter(Myclient.GetStream())
                {
                    AutoFlush = true
                };

                _stateObj = new StateObject
                {
                    workSocket = Myclient.Client
                };
                log.Debug("port TCP Ouvert avec " + IpAddress);
            }
            catch (SocketException ex)
            {
                log.Error("Port TCP indisponible " + IpAddress + " " + ex.Message);
                UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);
                return;
            }

            #endregion

            #region Open Protocol connexion

            log.Debug("Demande de Communication OP avec " + IpAddress);

            await SendAsync("0020", "0001", "003", "");
            string data = await ReceiveAsync();
            if (data.Substring(4, 4) == "0002")
            {
                ControlerInf = new ControllerInformation(data);

                /*if (ControlerInf.SupplierCode == "ACT")
                {
                    log.Debug("Demande de Communication OP aceptée avec " + IpAddress);
                }
                else
                {
                    log.Debug("Demande de Communication OP refusée avec " + IpAddress + " Suplier Code Error");
                    UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);
                    return;
                }*/
            }
            else
            {
                log.Debug("Demande de Communication OP refusée avec " + IpAddress + "réponse inconnue");
                UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);
                return;
            }

            log.Debug("Recupération information outil");

            switch (_marque)
            {
                case Marques.Dessouter:
                    await SendAsync("0020", "0040", "002", "");
                    break;
                case Marques.Stanley:
                    await SendAsync("0020", "0040", "003", "");
                    break;
                case Marques.Atlas:
                    await SendAsync("0020", "0040", "003", "");
                    break;
            }

            await SendAsync("0020", "0040", "003", "");
            data = await ReceiveAsync();
            if (data.Substring(4, 4) == "0041")
            {
                _ToolInformations = new ToolInformations(data);
                log.Info("Outil SN : " + _ToolInformations.ToolSerialNumber + " connecté");
            }
            else
            {
                UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);
                return;
            }

            #endregion

            #region Jobs

            PsetsnamePerJob = GetPsetsname();

            #endregion

            #region Description program

            ProgDescList = GetDescProgList();

            #endregion

            #region Souscription

            this.Ecoute();
            if (!(_Subscriptions is null))
            {
                foreach (var Subscription in _Subscriptions)
                {
                    switch (Subscription)
                    {
                        case Subscriptions.ConfigurationEquipment:
                            await ConfigurationEquipmentSubscribe();
                            break;
                        case Subscriptions.LasTightening:
                            await LastResultSubscribe();
                            break;
                        case Subscriptions.LastPowerMacsResult:
                            await SubscribeLastPowerMACSTighteningResultTest();
                            break;
                        case Subscriptions.LastCurve:
                            await SubscribeLastCurve();
                            break;
                        case Subscriptions.JobInformation:
                            await JobSubscribe();
                            break;
                        case Subscriptions.LastTracePlotInformation:
                            await SubscribeTracePlot();
                            break;
                        case Subscriptions.StatusMonitoredInput:
                            await SubscribeMonitoredInput();
                            break;
                        case Subscriptions.UserMessage:
                            await SubscibeUSerDataMessage();
                            break;
                        default:
                            break;
                    }

                    CommandAck.WaitOne();

                    CommandAck.Reset();
                }
            }

            #endregion

            this.Activation_KeepAlive();
            UpdateConnectionStatus(ConnexionStatusEnum.Connected);

            log.Info("************** FIN DE CONNEXION avec " + IpAddress + " **********************");
        }

        public void SendIdentifier(string Data)
        {
            if (Myclient == null) return;

            if (Myclient.Connected == true)
            {
                log.Info("PC-->CT : Envoi Identifiant :" + Data);
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                SendAsync((20 + Data.Length).ToString().PadLeft(4, '0'), "0150", "000", Data);
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            }
        }

        public void SendIdentifierPM(string Data)
        {
            if (Myclient == null) return;

            if (Myclient.Connected == true)
            {
                log.Info("PC-->CT : Envoi Identifiant :" + Data);
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                SendAsync((20 + Data.Length).ToString().PadLeft(4, '0'), "0050", "000", Data);
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            }
        }

        public void RazIdentifier()
        {
            if (Myclient == null) return;

            if (Myclient.Connected == true)
            {
                log.Info("PC-->PF : RazIdentifier");
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                SendAsync("0020", "0157", "000", "");
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            }
        }

        public void Activation_KeepAlive()
        {
            log.Info("KeepAlive Started");
            KeepAlivetimer.Start();
        }

        public async Task Disconnect()
        {
            if (Myclient != null && Myclient.Connected == true)
            {
                ClosePending = true;
                KeepAlivetimer.Stop();
                ConnectionWatchDog.Stop();
                TimeOut.Stop();
                await SendAsync("0020", "0003", "000", "");
                CommandAck.WaitOne();
                CommandAck.Reset();
                // Myclient.Client.Shutdown(SocketShutdown.Both);
                Myclient.Client.Close();
                log.Info("PC-->PF : *************** FIN DE COMUNICATION *************");
                UpdateConnectionStatus(ConnexionStatusEnum.Disconnected);
                ClosePending = false;
            }
        }

        private void Ecoute()
        {
            if (Myclient == null || _stateObj == null) return;

            try
            {
                // Begin receiving the data from the remote device if no close connexion pending.  
                if (_stateObj.workSocket != null)
                {
                    _stateObj.workSocket.BeginReceive(_stateObj.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(OnDataReceived), _stateObj);
                }
            }
            catch (Exception ex)
            {
                KeepAlivetimer.Stop();
                log.Info("Ecoute()" + IpAddress + "  " + ex.Message);
            }
        }

        public void AbortJob()
        {
            log.Info(": Abort job");
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            SendAsync("0020", "0127", "000", "");
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            //this.SyncReceive();
        }

        public void UnlockTool()
        {
            if (Myclient == null) return;

            if (Myclient.Connected == true)
            {
                log.Info(": Acquit Défaut");
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                SendAsync("0023", "0224", "000", "002"); // Set de Unlock tool
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                CommandAck.WaitOne();
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                SendAsync("0023", "0225", "000", "002"); // Reset de Unlock tool
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            }
        }

        public void SelectJob(int JobNum)
        {
            log.Info("PC->PF  : Selection Job " + JobNum.ToString());
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            SendAsync("0022", "0038", "000", JobNum.ToString().PadLeft(2, '0'));
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
        }

        public void SelectPset(int PsetID)
        {
            log.Info("PC->PF  : Selection Pset " + PsetID.ToString());
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            SendAsync("0023", "0018", "001", PsetID.ToString().PadLeft(3, '0'));
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
        }

        public async Task<Hashtable> GetJobList()
        {
            log.Info("PC-->PF : GetJobList()");
            Hashtable Jobname = new Hashtable();
            Jobname.Clear();
            string IDList;
            await SendAsync("0020", "0030", "000", "");
            string reponse = await this.ReceiveAsync();


            if (reponse.Substring(4, 4) == "0031")
            {
                IDList = reponse.Substring(22);

                for (int i = 0; i < IDList.Length / 2; i++)
                {
                    string ID = IDList.Substring(i * 2, 2);
                    await SendAsync("0022", "0032", "000", ID);
                    reponse = await ReceiveAsync();
                    if (reponse.Substring(4, 4) == "0033")
                    {
                        Jobname.Add(int.Parse(ID), reponse.Substring(26, 25).Trim());
                    }
                }
            }


            return Jobname;
        }
        public async Task<List<ProgDescription>> GetDescProgList()
        {
            log.Info("PC-->PF : GetDescProg()");

            string IDlist;
            List<ProgDescription> list = new List<ProgDescription>();
            await SendAsync("0020", "0010", "000", "");
            string reponse = await ReceiveAsync();
            if (reponse.Substring(4, 4) == "0011")
            {
                IDlist = reponse.Substring(23);

                for (int i = 0; i < IDlist.Length / 3; i++)
                {
                    string ID = IDlist.Substring(i * 3, 3);
                    await SendAsync("0023", "0012", "000", ID);
                    reponse = await ReceiveAsync();

                    if (reponse.Substring(4, 4) == "0013")
                    {
                        list.Add(new ProgDescription(reponse));
                    }
                }
            }
            return list;
        }

        public async Task<Hashtable> GetPsetsname()
        {
            log.Info("PC-->PF : GetPsetName()");
            Hashtable PsetNamedata = new Hashtable();
            string IDlist;
            await SendAsync("0020", "0010", "000", "");
            string reponse = await ReceiveAsync();
            if (reponse.Substring(4, 4) == "0011")
            {
                IDlist = reponse.Substring(23);

                for (int i = 0; i < IDlist.Length / 3; i++)
                {
                    string ID = IDlist.Substring(i * 3, 3);
                    await SendAsync("0023", "0012", "000", ID);
                    reponse = await ReceiveAsync();

                    if (reponse.Substring(4, 4) == "0013")
                    {
                        // PsetNamedata.Add(ID + ":" + reponse.Substring(27, 25) + ";");
                        // PsetNamedata.Insert(int.Parse(ID), reponse.Substring(27, 25));
                        PsetNamedata.Add(int.Parse(ID), reponse.Substring(27, 25));
                    }
                }
            }

            return PsetNamedata;
        }


        public async Task<int> GetPsetinJob(int Jobnumber)
        {
            int Numb_Pset = 0;
            string reponse;
            log.Info("PC-->PF : GetPsetinJob()");
            await SendAsync("0022", "0032", "000", Jobnumber.ToString().PadLeft(2, '0'));
            reponse = await ReceiveAsync();
            if (reponse.Substring(4, 4) == "0033")
            {
                Numb_Pset = int.Parse(reponse.Substring(87, 2));
            }

            return Numb_Pset;
        }

        public void GetJob_data(int Jobnumber)
        {
            log.Info("PC-->PF : GetJobData()");
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            SendAsync("0022", "0032", "000", Jobnumber.ToString().PadLeft(2, '0'));
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
        }

        public void Send_DynJob(string Data)
        {
            if (Myclient == null) return;

            if (Myclient.Connected == true)
            {
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                SendAsync((20 + Data.Length).ToString().PadLeft(4, '0'), "0140", "999", Data);
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                // logmefile.Newline(" PC-->PF : Envoi gamme dynamique :" + Data);
            }
        }

        public void Batchinc(int Step)
        {
            log.Info("PC-->PF : Batch increment " + Step.ToString() + " Steps");
            for (int i = 0; i < Step; i++)
            {
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                SendAsync("0020", "0128", "000", "");
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                CommandAck.WaitOne();
                //System.Threading.Thread.Sleep(200);
            }
        }

        public void DisableTool()
        {
            log.Info("PC-->PF : DisableTool()");
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            SendAsync("0020", "0042", "000", "");
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
        }

        public void EnableTool()
        {
            if (Myclient == null) return;

            log.Info("PC-->PF : EnableTool()");
            if (Myclient.Connected == true)
            {
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                SendAsync("0020", "0043", "000", "");
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            }
        }

        public void SetExtRelays(short RelayNumber, ExtRelayStatus RelayStatus, ExtRelayStatus other)
        {
            if (Myclient == null) return;

            StringBuilder sb = new StringBuilder();

            for (int i = 1; i < 11; i++)
            {
                if (RelayNumber == i)
                {
                    sb.Append((int)RelayStatus);
                }
                else
                {
                    sb.Append((int)other);
                }
            }


            string data = sb.ToString();

            if (Myclient.Connected == true)
            {
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                SendAsync("0030", "0200", "000", data);
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            }
        }

        public void UserDataDownload(byte[] data)
        {
            if (Myclient == null) return;

            string hex = BitConverter.ToString(data).Replace("-", string.Empty);

            if (Myclient.Connected == true &&
                LastUserMessage != hex) // test si dernier message différent pour eviter Flooding com OP
            {
                log.Info("PC-->PF : Mise à jour sortie OP : " + hex);
                int Mlength = 20 + hex.Length;
                LastUserMessage = hex;
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                SendAsync(Mlength.ToString().PadLeft(4, '0'), "0240", "000", hex);
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            }
        }

        public void FlashGreenLight()
        {
            if (Myclient == null) return;

            log.Info("PC-->PF : FlashGreenLite()");
            if (Myclient.Connected == true)
            {
#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
                SendAsync("0020", "0113", "001", "");
#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel. Envisagez d'appliquer l'opérateur 'await' au résultat de l'appel.
            }
        }

        #endregion
    }

    class StateObject
    {
        // Client socket.  
        public Socket? workSocket = null;

        // Size of receive buffer.  
        public const int BufferSize = 65000;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        public bool PartialMessage = false;
        public int EndIndex = 0;
        public int RxBytesCount = 0;
        public int FullMsgLen = 0;

        // Received data string.  
        public List<Byte> BytesMessage = new List<byte>();
    }
}