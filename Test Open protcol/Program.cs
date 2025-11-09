// See https://aka.ms/new-console-template for more information
using OpenProtocol;
using OpenProtocol.OpenProtocolTypes;
using log4net.Config;
using System.IO;
using NetworkLayer;

// Configure log4net
XmlConfigurator.Configure(new FileInfo("log4net.config"));

Console.WriteLine("Connecting to server.");

List<Subscriptions> subscriptions = new List<Subscriptions>()
{
    Subscriptions.LasTightening,
    Subscriptions.JobInformation, 
};

NetworkLayer.TcpClientLayer tcpClient = new NetworkLayer.TcpClientLayer();

tcpClient.MessageReceived += (message) =>
{
    Console.WriteLine($"New message received: {message}");
};

tcpClient.Connected += () =>
{
    
    Console.WriteLine("Connected to server.");
};

tcpClient.ErrorOccurred += (exception) =>
{
    Console.WriteLine($"Error occurred: {exception.Message}");
};

tcpClient.Disconnected += () =>
{
    Console.WriteLine("Disconnected from server.");
};

await tcpClient.ConnectAsync("192.168.1.20", 4545);

await tcpClient.SendAsync(OpMessage.Build("0020", "0001", "003", ""));


Console.WriteLine("Press any key to exit...");
Console.Read();

// void OPClient_NewResultEvt(object? sender, ResultEvtArgs e)
//{
//    Console.WriteLine($"New message received: {e.ToString()}");
//}

