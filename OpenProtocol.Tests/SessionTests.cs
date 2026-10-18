using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using OpenProtocol.Exceptions;
using OpenProtocol.OpenProtocolTypes;
using OpenProtocol.Services;

namespace OpenProtocol.Tests
{
    public class SessionTests
    {
        [Fact(Timeout = 10000)]
        public async Task TwoResultAcksAreNotInterleaved()
        {
            using var listener = StartListener(out var port);
            using var client = new OpenProtocolV2();
            await using var server = await HandshakeAsync(listener, client, port);
            var packet = WithNul(ResultMessage(1));
            packet.AddRange(WithNul(ResultMessage(2)));
            await server.Stream.WriteAsync(packet.ToArray());

            var frames = await ReadFramesAsync(server.Stream, 2, TimeSpan.FromSeconds(2));

            Assert.Equal(2, frames.Count);
            Assert.All(frames, frame =>
            {
                Assert.Equal("0062", frame.Substring(4, 4));
                Assert.Equal(frame.Length, int.Parse(frame[..4], CultureInfo.InvariantCulture));
            });
        }

        [Fact(Timeout = 10000)]
        public async Task HandlerCanSendWhilePreviousAckCompletes()
        {
            using var listener = StartListener(out var port);
            using var layer = new TcpClientLayer();
            var nested = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            layer.MessageReceived += message =>
            {
                if (message.Mid != "0005")
                {
                    return;
                }

                var reply = layer.SendAndWaitAckAsync(
                    OpMessage.Build(Mids.KeepAlive, 0, ""),
                    new[] { "9999" },
                    timeoutMs: 2000).GetAwaiter().GetResult();
                nested.TrySetResult(reply.Mid);
            };

            var accepted = listener.AcceptTcpClientAsync();
            await layer.ConnectAsync(IPAddress.Loopback.ToString(), port);
            using var server = await accepted;
            await using var stream = server.GetStream();

            var waiting = layer.SendAndWaitAckAsync(
                OpMessage.Build(Mids.CommunicationStart, 1, ""),
                new[] { "0005" },
                timeoutMs: 2000);

            var first = await ReadFrameAsync(stream, TimeSpan.FromSeconds(2));
            Assert.Equal("0001", first.Substring(4, 4));
            await WriteFrameAsync(stream, Frame("0005"));

            Assert.Equal("0005", (await waiting).Mid);
            var second = await ReadFrameAsync(stream, TimeSpan.FromSeconds(2));
            Assert.Equal("9999", second.Substring(4, 4));
            await WriteFrameAsync(stream, Frame("9999", "000"));

            Assert.Equal("9999", await nested.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        }

        [Fact(Timeout = 10000)]
        public async Task CloseWhileWaitingIsNotATimeout()
        {
            using var listener = StartListener(out var port);
            using var layer = new TcpClientLayer();
            var accepted = listener.AcceptTcpClientAsync();
            await layer.ConnectAsync(IPAddress.Loopback.ToString(), port);
            using var server = await accepted;
            await using var stream = server.GetStream();

            var waiting = layer.SendAndWaitAckAsync(
                OpMessage.Build(Mids.CommunicationStart, 1, ""),
                new[] { "0005" },
                timeoutMs: 5000);
            await ReadFrameAsync(stream, TimeSpan.FromSeconds(2));
            server.Close();

            var error = await Assert.ThrowsAnyAsync<IOException>(() => waiting);
            Assert.Contains("Connection closed", error.Message, StringComparison.Ordinal);
        }

        [Fact(Timeout = 10000)]
        public async Task RefusedSubscriptionClosesTheSocket()
        {
            using var listener = StartListener(out var port);
            using var client = new OpenProtocolV2();
            var accepted = listener.AcceptTcpClientAsync();
            var connecting = client.ConnectAsync(IPAddress.Loopback.ToString(), port, new List<Subscription>
            {
                new() { Type = SubTypes.SubscribeToResults, Rev = 1 }
            });
            using var server = await accepted;
            await using var stream = server.GetStream();

            await ReadFrameAsync(stream, TimeSpan.FromSeconds(2));
            await WriteFrameAsync(stream, Frame("0002"));
            await ReadFrameAsync(stream, TimeSpan.FromSeconds(2));
            await WriteFrameAsync(stream, Frame("0004"));

            await Assert.ThrowsAsync<ProtocolException>(() => connecting);
            Assert.False(client.IsConnected);
        }

        [Fact(Timeout = 10000)]
        public async Task ShortResultIsNotAcknowledged()
        {
            using var listener = StartListener(out var port);
            using var client = new OpenProtocolV2();
            var raised = false;
            var failed = false;
            client.NewResultEvt += _ => raised = true;
            client.ErrorOccurred += _ => failed = true;
            await using var server = await HandshakeAsync(listener, client, port);
            await WriteFrameAsync(server.Stream, Frame("0061"));
            var sawAck = await SawMidAsync(server.Stream, "0062", TimeSpan.FromMilliseconds(500));

            Assert.False(raised);
            Assert.True(failed);
            Assert.False(sawAck);
        }

        [Fact(Timeout = 10000)]
        public async Task TighteningIdZeroIsDeliveredOnce()
        {
            using var listener = StartListener(out var port);
            using var client = new OpenProtocolV2();
            var ids = new List<int>();
            client.NewResultEvt += result =>
            {
                lock (ids)
                {
                    ids.Add(result.TightId);
                }
            };
            await using var server = await HandshakeAsync(listener, client, port);
            await WriteFrameAsync(server.Stream, ResultMessage(0));
            await WaitUntil(() => Count(ids) >= 1);

            await WriteFrameAsync(server.Stream, ResultMessage(0));
            await Task.Delay(300);
            Assert.Equal(1, Count(ids));

            await WriteFrameAsync(server.Stream, ResultMessage(1));
            await WaitUntil(() => Count(ids) >= 2);
            Assert.Equal(new[] { 0, 1 }, Snapshot(ids));
        }

        [Fact]
        public void DisposeIsIdempotent()
        {
            var client = new OpenProtocolV2();

            client.Dispose();
            client.Dispose();
        }

        private static int Count(List<int> ids)
        {
            lock (ids)
            {
                return ids.Count;
            }
        }

        private static int[] Snapshot(List<int> ids)
        {
            lock (ids)
            {
                return ids.ToArray();
            }
        }

        private static async Task WaitUntil(Func<bool> condition)
        {
            var started = Environment.TickCount64;
            while (!condition())
            {
                if (Environment.TickCount64 - started > 2000)
                {
                    throw new TimeoutException("Timed out waiting for a session event.");
                }

                await Task.Delay(20);
            }
        }

        private static TcpListener StartListener(out int port)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            port = ((IPEndPoint)listener.LocalEndpoint).Port;
            return listener;
        }

        private static async Task<ServerLink> HandshakeAsync(TcpListener listener, OpenProtocolV2 client, int port)
        {
            var accepted = listener.AcceptTcpClientAsync();
            var connecting = client.ConnectAsync(IPAddress.Loopback.ToString(), port, new List<Subscription>
            {
                new() { Type = SubTypes.SubscribeToResults, Rev = 1 }
            });
            var server = await accepted;
            var stream = server.GetStream();
            await ReadFrameAsync(stream, TimeSpan.FromSeconds(2));
            await WriteFrameAsync(stream, Frame("0002"));
            await ReadFrameAsync(stream, TimeSpan.FromSeconds(2));
            await WriteFrameAsync(stream, Frame("0005"));
            await connecting;
            return new ServerLink(server, stream);
        }

        private static string Frame(string mid, string revision = "001", string data = "")
        {
            var rev = revision.Length >= 3 ? revision[^3..] : revision.PadLeft(3, '0');
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:D4}{1}{2}         {3}",
                20 + data.Length,
                mid,
                rev,
                data);
        }

        private static string ResultMessage(int tightId)
        {
            var chars = new char[320];
            Array.Fill(chars, '0');
            "0061".CopyTo(chars.AsSpan(4));
            "001".CopyTo(chars.AsSpan(8));
            tightId.ToString("D9", CultureInfo.InvariantCulture).CopyTo(chars.AsSpan(304));
            chars.Length.ToString("D4", CultureInfo.InvariantCulture).CopyTo(chars.AsSpan(0));
            return new string(chars);
        }

        private static List<byte> WithNul(string ascii)
        {
            var bytes = Encoding.ASCII.GetBytes(ascii).ToList();
            bytes.Add(0);
            return bytes;
        }

        private static async Task WriteFrameAsync(NetworkStream stream, string ascii)
        {
            var bytes = WithNul(ascii);
            await stream.WriteAsync(bytes.ToArray());
        }

        private static async Task<string> ReadFrameAsync(NetworkStream stream, TimeSpan timeout)
        {
            var frames = await ReadFramesAsync(stream, 1, timeout);
            return frames[0];
        }

        private static async Task<List<string>> ReadFramesAsync(NetworkStream stream, int count, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var frames = new List<string>(count);
            var current = new List<byte>();
            var one = new byte[1];
            while (frames.Count < count)
            {
                var read = await stream.ReadAsync(one.AsMemory(0, 1), cts.Token);
                if (read == 0)
                {
                    throw new IOException("Connection closed while reading a frame.");
                }

                if (one[0] == 0)
                {
                    frames.Add(Encoding.ASCII.GetString(current.ToArray()));
                    current.Clear();
                    continue;
                }

                current.Add(one[0]);
            }

            return frames;
        }

        private static async Task<bool> SawMidAsync(NetworkStream stream, string mid, TimeSpan timeout)
        {
            try
            {
                var frame = await ReadFrameAsync(stream, timeout);
                return frame.Length >= 8 && frame.Substring(4, 4) == mid;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private sealed class ServerLink : IAsyncDisposable
        {
            public ServerLink(TcpClient client, NetworkStream stream)
            {
                Client = client;
                Stream = stream;
            }

            public TcpClient Client { get; }
            public NetworkStream Stream { get; }

            public async ValueTask DisposeAsync()
            {
                await Stream.DisposeAsync();
                Client.Dispose();
            }
        }
    }
}
