// See https://aka.ms/new-console-template for more information

using Aspdcs.Rtu.DLT645;
//using Aspdcs.Rtu.DLT645.Extensions;
using Aspdcs.Rtu.DLT645.Serialization;
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
    .WithAuto()
    .Run();

// 使用默认配置创建客户端
//var options = ChannelOptions.CreateDefaultBuilder()
//    .WithChannel("COM5")
//    .Build();

//Dlt645Client client = new(options);


List<AddressValue> addresses = [];
// 广播获取所有电表设备地址
await foreach (var address in client.TryReadAddressAsync())
{
    addresses.Add(address);
}

// 读取所有电表总(正向有功)电量数据
await foreach (var frame in client.ReadAsync("111100000000"))
{
    Console.WriteLine($"Received: {frame}");
}
//// 读取 1997 版电表总(正向有功)电量数据
//await foreach (var frame in client.Read1997Async("111100000000"))
//{
//    Console.WriteLine($"Received: {frame}");
//}
Console.WriteLine("Hello, World!");
