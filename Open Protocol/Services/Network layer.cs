using System.Net.Sockets;
using System.Text;
using OpenProtocol.OpenProtocolTypes;

namespace OpenProtocol.Services
{
    public class TcpClientLayer : IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private readonly Encoding _encoding = Encoding.ASCII;
        private readonly object _ackLock = new();
        private readonly SemaphoreSlim _commandGate = new(1, 1);

        private TaskCompletionSource<IncomingMessage>? _ackTcs;
        private HashSet<string> _expectedAckMids = new();
        private int _disconnected;

        public bool IsConnected => _client?.Connected == true && _disconnected == 0;

        public event Action? Connected;
        public event Action? Disconnected;
        public event Action<IncomingMessage>? MessageReceived;
        public event Action<string>? MessageSent;
        public event Action<Exception>? ErrorOccurred;

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            if (_client is not null || _stream is not null || _cts is not null)
            {
                Disconnect();
            }

            _disconnected = 0;

            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(host, port, cancellationToken);
                _stream = _client.GetStream();
                _cts = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
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
            if (!IsConnected || _stream is null)
            {
                throw new InvalidOperationException("Not connected to server.");
            }

            try
            {
                var data = _encoding.GetBytes(message);
                var buffer = new byte[data.Length + 1];
                Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
                buffer[^1] = OpenProtocolFramer.MessageTerminator;

                await _stream.WriteAsync(buffer, cancellationToken);
                await _stream.FlushAsync(cancellationToken);
                MessageSent?.Invoke(message);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
                Disconnect();
                throw;
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
                _commandGate.Release();
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

        private void Dispatch(IncomingMessage incoming)
        {
            MessageReceived?.Invoke(incoming);

            lock (_ackLock)
            {
                if (_ackTcs is not null
                    && !_ackTcs.Task.IsCompleted
                    && _expectedAckMids.Contains(incoming.Mid))
                {
                    _ackTcs.TrySetResult(incoming);
                }
            }
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
                    _ackTcs?.TrySetCanceled();
                    _ackTcs = null;
                    _expectedAckMids.Clear();
                }

                _cts = null;
                _stream = null;
                _client = null;
                Disconnected?.Invoke();
            }
        }

        public void Dispose()
        {
            Disconnect();
            _commandGate.Dispose();
        }
    }
}
