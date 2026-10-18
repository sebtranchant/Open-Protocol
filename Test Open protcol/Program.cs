using log4net.Config;
using OpenProtocol.OpenProtocolTypes;
using OpenProtocol.Services;

XmlConfigurator.Configure(new FileInfo("log4net.config"));

List<Subscription> subscriptions =
[
    new() { Type = SubTypes.SubscribeToResults, Rev = 4 },
    new() { Type = SubTypes.SubsribeToJobInfo, Rev = 4 }
];

Console.WriteLine("Connecting to server.");

using var opClient = new OpenProtocolV2();
opClient.Connected += () => Console.WriteLine("Connected to controller.");
opClient.Disconnected += () => Console.WriteLine("Disconnected from controller.");
opClient.ErrorOccurred += ex => Console.WriteLine($"Error: {ex.Message}");
opClient.NewResultEvt += result => Console.WriteLine($"New tightening received: {result.Status}");
opClient.NewJobEvt += job => Console.WriteLine($"New job received: {job.JobState}");

try
{
    await opClient.ConnectAsync("192.168.1.20", 4545, subscriptions);
    if (opClient.ControlerInf is not null)
    {
        Console.WriteLine($"Controller: {opClient.ControlerInf.ControllerName} ({opClient.ControlerInf.ControllerSerial})");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
}

Console.WriteLine("Press any key to exit...");
Console.Read();
await opClient.DisconnectAsync();
