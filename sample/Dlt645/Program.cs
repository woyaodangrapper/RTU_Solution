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


//IDlt645Client client = ChannelOptions.CreateBuilder("MyChannel")
//    .WithChannel("COM5")
//    .WithAuto()
//    .Run();

// 使用默认配置创建客户端
//var options = ChannelOptions.CreateDefaultBuilder()
//    .WithChannel("COM5")
//    .Build();

//var client = new Dlt645Client(options)
//    .ConnectAsync();

// 使用工厂创建客户端
//var factory = new Dlt645ClientFactory();
//var client = factory.CreateDlt645Client();
//await client.ConnectAsync(options);

//List<AddressValue> addresses = [];
//// 广播获取所有电表设备地址
//await foreach (var address in client.TryReadAddressAsync())
//{
//    addresses.Add(address);
//}

//// 读取所有电表总(正向有功)电量数据
//await foreach (var frame in client.ReadAsync("111100000000"))
//{
//    Console.WriteLine($"Received: {frame}");
//}
//// 读取 1997 版电表总(正向有功)电量数据
//await foreach (var frame in client.Read1997Async("111100000000"))
//{
//    Console.WriteLine($"Received: {frame}");
//}

{
    var client = ChannelOptions.CreateDefaultBuilder()
        .WithChannel("COM5")
        .Run();

    uint[] dataItems =
    [
        0x00010000,  // 正向有功总电能
        0x02010100,  // A 相电压
        0x02020100   // A 相电流
    ];

    foreach (var dataId in dataItems)
    {
        await foreach (var frame in client.ReadAsync("111100000000", dataId))
        {
            Console.WriteLine($"数据标识 {dataId:X8}: {frame}");
        }
    }

}
Console.WriteLine("Hello, World!");
