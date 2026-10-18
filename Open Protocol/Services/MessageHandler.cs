using System;
using OpenProtocol.OpenProtocolTypes;
using OpenProtocol.Services;


namespace Open_Protocol.Services
{

    public class MessageHandler
    {
        private readonly TcpClientLayer _tcpClientLayer;

        public MessageHandler(TcpClientLayer tcpClientLayer)
        {
            _tcpClientLayer = tcpClientLayer;
            _tcpClientLayer.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(IncomingMessage message)
        {
            Console.WriteLine($"Message received: {message.Mid}");
        }


       // public async Task SendMessageAsync(Subscriptions message, string rev, string data)
        //{
       //     string builtMessage = OpMessage.Build(message, rev, data);
       //     await _tcpClientLayer.SendAsync(builtMessage);
        //}

        

    }
}