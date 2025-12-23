// See https://aka.ms/new-console-template for more information

using Aspdcs.Rtu.DLT645;
using Microsoft.Extensions.Logging;
using System.Diagnostics;


// 运行示例代码
var console = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Trace);
});


IDlt645Client client = ChannelOptions.CreateBuilder("MyChannel")
    .WithChannel("COM5")
    .WithAuto()
    .Run();

//var options = ChannelOptions.CreateDefaultBuilder()
//    .WithChannel("COM5")
//    .Build();

//Dlt645Client client = new(options);


var sw = Stopwatch.StartNew();

await foreach (var address in client.TryReadAddressAsync())
{
    Console.WriteLine($"addresses: {address}");
}

sw.Stop();
Console.WriteLine($"TryReadAddressAsync total elapsed: {sw.ElapsedMilliseconds} ms");


sw.Restart();
await foreach (var frame in client.ReadAsync("11-11-00-00-00-00"))
{
    Console.WriteLine($"Received: {frame}");
}

sw.Stop();
Console.WriteLine($"ReadAsync total elapsed: {sw.ElapsedMilliseconds} ms");


Console.WriteLine("Hello, World!");
