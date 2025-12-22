// See https://aka.ms/new-console-template for more information

using Aspdcs.Rtu.DLT645;
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

await foreach (var address in channel.TryReadAddressAsync())
{
    Console.WriteLine($"addresses: {address}");
}

Console.WriteLine("Starting read operation without CancellationToken (using internal timeout protection)...");
await foreach (var frame in channel.ReadAsync("11-11-00-00-00-00"))
{
    Console.WriteLine($"Received: {frame}");
}


Console.WriteLine("Hello, World!");
