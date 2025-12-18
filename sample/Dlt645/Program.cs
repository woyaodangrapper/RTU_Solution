// See https://aka.ms/new-console-template for more information

using Aspdcs.Rtu.DLT645.Contracts;
using Aspdcs.Rtu.DLT645.Serialization;
using Microsoft.Extensions.Logging;


var console = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Trace);
});


var channel = new CreateBuilder("MyChannel")
    .WithChannel("COM5")
    .WithLogger(console)
    .Run();

//var client = new Dlt645Client(channelOptions, console);
//await foreach (var frame in await channel.TryReadAddressAsync())
//{
//    Console.WriteLine($"Received address: {BitConverter.ToString(frame.Address)}");
//}

try
{
    await foreach (var frame in channel.TrySendAsync(Command.EnergyData.ForwardActiveTotalEnergy, "11-11-00-00-00-00"))
    {
        Console.WriteLine($"{frame}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Caught exception: {ex}");
}

Console.WriteLine("Hello, World!");

Console.ReadLine();
