// See https://aka.ms/new-console-template for more information

using Aspdcs.Rtu.DLT645;
using Microsoft.Extensions.Logging;


// 运行示例代码
var console = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Trace);
});


IDlt645Client client = ChannelOptions.CreateBuilder("MyChannel")
    .WithChannel("COM5")
    .Run();

//var options = ChannelOptions.CreateDefaultBuilder()
//    .WithChannel("COM5")
//    .Build();

//Dlt645Client client = new(options);


await foreach (var address in client.TryReadAddressAsync())
{
    Console.WriteLine($"addresses: {address}");
}

Console.WriteLine("Starting read operation without CancellationToken (using internal timeout protection)...");
await foreach (var frame in client.ReadAsync("11-11-00-00-00-00"))
{
    Console.WriteLine($"Received: {frame}");
}


Console.WriteLine("Hello, World!");
