// See https://aka.ms/new-console-template for more information

using Asprtu.Rtu.DLT645.Contracts;
using Asprtu.Rtu.DLT645.Serialization;
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

await foreach (var frame in channel.TrySendAsync(Command.EnergyData.None, "11-11-00-00-00-00"))
{
    Console.WriteLine($"{BitConverter.ToString(frame.ToBytes())}");
}

Console.WriteLine("Hello, World!");
