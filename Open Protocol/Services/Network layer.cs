using System.Net.Sockets;
using System.Threading.Channels;
using System.Text;
using OpenProtocol.OpenProtocolTypes;

namespace OpenProtocol.Services
{
    public class TcpClientLayer : IDisposable
    {
        private readonly SemaphoreSlim _writeGate = new(1, 1);
        private readonly SemaphoreSlim _commandGate = new(1, 1);
        private readonly object _ackLock = new();
        private readonly Encoding _encoding = Encoding.ASCII;

        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private Channel<IncomingMessage> _inbox = Channel.CreateUnbounded<IncomingMessage>();
        private Task? _receiveTask;
        private Task? _processTask;
        private TaskCompletionSource<IncomingMessage>? _ackTcs;
        private HashSet<string> _expectedAckMids = new();
        private int _disconnected = 1;
        private int _disposed;

        public bool IsConnected => _client?.Connected == true && _disconnected == 0;

        public event Action? Connected;
        public event Action? Disconnected;
        public event Action<IncomingMessage>? MessageReceived;
        public event Action<string>? MessageSent;
        public event Action<Exception>? ErrorOccurred;

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            Disconnect();
            await WaitForLoopsAsync();

            _disconnected = 0;
            _inbox = Channel.CreateUnbounded<IncomingMessage>();
            var inbox = _inbox;

            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(host, port, cancellationToken);
                _stream = _client.GetStream();
                _cts = new CancellationTokenSource();
                var token = _cts.Token;
                _receiveTask = Task.Run(() => ReceiveLoopAsync(token));
                _processTask = Task.Run(() => ProcessLoopAsync(inbox, token));
                Connected?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
                Disconnect();
                throw new InvalidOperationException("Failed to connect to server.", ex);
            }
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            var acquired = false;
            try
            {
                await _writeGate.WaitAsync(cancellationToken);
                acquired = true;

                if (!IsConnected || _stream is null)
                {
                    throw new InvalidOperationException("Not connected to server.");
                }

                var data = _encoding.GetBytes(message);
                var buffer = new byte[data.Length + 1];
                Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
                buffer[^1] = OpenProtocolFramer.MessageTerminator;

                await _stream.WriteAsync(buffer, cancellationToken);
                await _stream.FlushAsync(cancellationToken);
                MessageSent?.Invoke(message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException && ex is not InvalidOperationException)
            {
                ErrorOccurred?.Invoke(ex);
                Disconnect();
                throw;
            }
            finally
            {
                if (acquired)
                {
                    Release(_writeGate);
                }
            }
        }

        public async Task<IncomingMessage> SendAndWaitAckAsync(
            string message,
            IReadOnlyCollection<string> expectedAckMids,
            int timeoutMs = 5000,
            CancellationToken cancellationToken = default)
        {
            if (expectedAckMids is null || expectedAckMids.Count == 0)
            {
                throw new ArgumentException("At least one expected ACK MID is required.", nameof(expectedAckMids));
            }

            await _commandGate.WaitAsync(cancellationToken);
            try
            {
                var tcs = new TaskCompletionSource<IncomingMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                lock (_ackLock)
                {
                    _expectedAckMids = new HashSet<string>(expectedAckMids, StringComparer.Ordinal);
                    _ackTcs = tcs;
                }

                try
                {
                    await SendAsync(message, cancellationToken);

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(timeoutMs);

                    await using (timeoutCts.Token.Register(() => tcs.TrySetCanceled(timeoutCts.Token)))
                    {
                        return await tcs.Task;
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"ACK not received within {timeoutMs}ms. Expected MID(s): {string.Join(",", expectedAckMids)}.");
                }
                finally
                {
                    lock (_ackLock)
                    {
                        if (ReferenceEquals(_ackTcs, tcs))
                        {
                            _ackTcs = null;
                            _expectedAckMids.Clear();
                        }
                    }
                }
            }
            finally
            {
                Release(_commandGate);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                var buffer = new byte[4096];
                var messageBuffer = new List<byte>(4096);

                while (!token.IsCancellationRequested && _stream is not null)
                {
                    var bytesRead = await _stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    messageBuffer.AddRange(buffer.AsSpan(0, bytesRead).ToArray());
                    while (OpenProtocolFramer.TryExtract(messageBuffer, out var incoming) && incoming is not null)
                    {
                        Dispatch(incoming);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
            finally
            {
                Disconnect();
            }
        }

        private async Task ProcessLoopAsync(Channel<IncomingMessage> inbox, CancellationToken token)
        {
            try
            {
                await foreach (var incoming in inbox.Reader.ReadAllAsync(token))
                {
                    try
                    {
                        MessageReceived?.Invoke(incoming);
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.Invoke(ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ChannelClosedException)
            {
            }
        }

        private void Dispatch(IncomingMessage incoming)
        {
            lock (_ackLock)
            {
                if (_ackTcs is not null
                    && !_ackTcs.Task.IsCompleted
                    && _expectedAckMids.Contains(incoming.Mid))
                {
                    _ackTcs.TrySetResult(incoming);
                }
            }

            _inbox.Writer.TryWrite(incoming);
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)
            {
                return;
            }

            try
            {
                _cts?.Cancel();
                _stream?.Close();
                _client?.Close();
            }
            catch
            {
            }
            finally
            {
                lock (_ackLock)
                {
                    _ackTcs?.TrySetException(new IOException("Connection closed while waiting for ACK."));
                    _ackTcs = null;
                    _expectedAckMids.Clear();
                }

                _inbox.Writer.TryComplete();
                _cts = null;
                _stream = null;
                _client = null;
                Disconnected?.Invoke();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            Disconnect();
            WaitForLoopsAsync().GetAwaiter().GetResult();

            _writeGate.Dispose();
            _commandGate.Dispose();
        }

        private async Task WaitForLoopsAsync()
        {
            var pending = new List<Task>(2);
            AddOtherLoop(_receiveTask, pending);
            AddOtherLoop(_processTask, pending);

            if (pending.Count == 0)
            {
                return;
            }

            try
            {
                await Task.WhenAll(pending).WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch
            {
            }
        }

        private static void AddOtherLoop(Task? loop, List<Task> pending)
        {
            if (loop is null || loop.IsCompleted)
            {
                return;
            }

            if (Task.CurrentId is int currentId && loop.Id == currentId)
            {
                return;
            }

            pending.Add(loop);
        }

        private void Release(SemaphoreSlim gate)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            try
            {
                gate.Release();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
