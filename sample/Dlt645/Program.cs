// See https://aka.ms/new-console-template for more information

using Asprtu.Rtu.DLT645;
using Asprtu.Rtu.DLT645.Contracts;
using Microsoft.Extensions.Logging;


var console = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Trace);
});


var channelOptions = new CreateBuilder("MyChannel")
    .WithChannel("COM5")
    .Build();

var client = new Dlt645Client(channelOptions, console);
await foreach (var frame in await client.TryReadAddressAsync())
{
    Console.WriteLine($"Received address: {BitConverter.ToString(frame.Address)}");
}
Console.WriteLine("Hello, World!");
