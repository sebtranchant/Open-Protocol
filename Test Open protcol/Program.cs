// See https://aka.ms/new-console-template for more information
using System.IO;
using log4net.Config;
using OpenProtocol;
using OpenProtocol.OpenProtocolTypes;
using OpenProtocol.Services;

// Configure log4net
XmlConfigurator.Configure(new FileInfo("log4net.config"));


List<Subscription> subscriptions = new List<Subscription>()
{
    new() { Type = SubTypes.SubscribeToResults, Rev = 4 },
    new() { Type = SubTypes.SubsribeToJobInfo, Rev = 4 }
};

Console.WriteLine("Connecting to server.");

OpenProtocolV2 OPClient = new OpenProtocolV2();
OPClient.NewResultEvt += (sender) =>
{
    Console.WriteLine($"New tightening received: {sender.Status}");
};
OPClient.NewJobEvt += (sender) =>
{
    Console.WriteLine($"New job received: {sender.JobState}");
};

try
{
    await OPClient.connectAsync("192.168.1.20", 4545, subscriptions);
}
catch (Exception ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
}



Console.WriteLine("Press any key to exit...");
Console.Read();

// void OPClient_NewResultEvt(object? sender, ResultEvtArgs e)
//{
//    Console.WriteLine($"New message received: {e.ToString()}");
//} 

