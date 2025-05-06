using Microsoft.Extensions.Logging;
using RTU.Infrastructures.Contracts.Tcp;
using RTU.TcpClient;
using RTU.TcpClient.Contracts;
using RTU.TcpServer;
using System.Runtime.InteropServices;
using System.Text.Json;


var console = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Trace);
});
int size = Marshal.SizeOf<MessageHeader>();
Console.WriteLine("hello world");

var tcpServer = new TcpServer(new("123", "127.0.0.1", 6688), console);

_ = Task.Run(async () => await tcpServer.TryExecuteAsync());


TcpClientFactory factory = new(console, new("default", "127.0.0.1", 6688));

ITcpClient dataClient = factory.CreateTcpClient();

dataClient.OnMessage += (client, data) =>
{
    Console.WriteLine($"OnMessage: " + ByteConverter.GetObject<string>(data));


    //data.WriteAsTable();
};
dataClient.OnSuccess += (client) =>
{
    Console.WriteLine($"OnSuccess");
};

_ = Task.Run(async () => await dataClient.TryExecuteAsync());



Console.ReadLine();
tcpServer.TrySendAsync(JsonSerializer.Serialize(new { name = "111" }));
Console.ReadLine();
tcpServer.TrySendAsync("123465");
Console.ReadLine();
