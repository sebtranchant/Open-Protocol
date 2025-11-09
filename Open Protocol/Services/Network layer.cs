using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkLayer
{
    public class TcpClientLayer : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private readonly byte _messageTerminator = 0x00; // Null byte
        private readonly Encoding _encoding = Encoding.ASCII;

        public bool IsConnected => _client?.Connected ?? false;

        // Events for external handling
        public event Action Connected;
        public event Action Disconnected;
        public event Action<string> MessageReceived;
        public event Action<Exception> ErrorOccurred;

        /// <summary>
        /// Connects asynchronously to a TCP server.
        /// </summary>
        public async Task ConnectAsync(string host, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();

                _cts = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                Connected?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
                Disconnect();
            }
        }

        /// <summary>
        /// Sends a message terminated by a null character.
        /// </summary>
        public async Task SendAsync(string message)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to server.");

            try
            {
                byte[] data = _encoding.GetBytes(message);
                byte[] buffer = new byte[data.Length + 1]; // +1 for null terminator
                Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
                buffer[^1] = _messageTerminator;

                await _stream.WriteAsync(buffer, 0, buffer.Length);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
                Disconnect();
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                byte[] buffer = new byte[1024];
                var messageBuffer = new List<byte>();

                while (!token.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead == 0)
                    {
                        // Server disconnected
                        break;
                    }

                    for (int i = 0; i < bytesRead; i++)
                    {
                        byte b = buffer[i];
                        if (b == _messageTerminator)
                        {
                            string message = _encoding.GetString(messageBuffer.ToArray());
                            messageBuffer.Clear();
                            MessageReceived?.Invoke(message);
                        }
                        else
                        {
                            messageBuffer.Add(b);
                        }
                    }
                }
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

        /// <summary>
        /// Disconnects the client cleanly.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _cts?.Cancel();
                _stream?.Close();
                _client?.Close();
            }
            catch { }
            finally
            {
                _cts = null;
                _stream = null;
                _client = null;
                Disconnected?.Invoke();
            }
        }

        public void Dispose() => Disconnect();
    }
}
