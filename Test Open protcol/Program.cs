// See https://aka.ms/new-console-template for more information
using OpenProtocol;
using OpenProtocol.OpenProtocolTypes;
using log4net.Config;
using System.IO;

// Configure log4net
XmlConfigurator.Configure(new FileInfo("log4net.config"));

List<Subscriptions> subscriptions = new List<Subscriptions>()
{
    Subscriptions.LasTightening,
    Subscriptions.JobInformation, 
};
OpenProtocol.OPClient oPClient = new OPClient("192.168.0.18",4545,subscriptions);
oPClient.ConnectAsync().Wait();
oPClient.NewResultEvt += OPClient_NewResultEvt;

Console.WriteLine("Press any key to exit...");
Console.ReadKey();

void OPClient_NewResultEvt(object? sender, ResultEvtArgs e)
{
    Console.WriteLine($"New message received: {e.ToString()}");
}

