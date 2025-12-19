// See https://aka.ms/new-console-template for more information

using Aspdcs.Rtu.DLT645.Contracts;
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
    // 不传递 CancellationToken，测试 Dlt645Client 内部的兜底超时保护
    Console.WriteLine("Starting read operation without CancellationToken (using internal timeout protection)...");
    await foreach (var frame in channel.ReadAsync("11-11-00-00-00-00"))
    {
        Console.WriteLine($"Received: {frame}");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation timed out (internal timeout protection worked!)");
}
catch (Exception ex)
{
    Console.WriteLine($"Caught exception: {ex.GetType().Name}: {ex.Message}");
}

Console.WriteLine("Hello, World!");

Console.ReadLine();
