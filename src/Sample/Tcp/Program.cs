using Microsoft.Extensions.Logging;
using RTU.TcpClient;
using RTU.TcpClient.Contracts;
using RTU.TcpServer;


var console = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Trace);
});

Console.WriteLine("hello world");
// 线程安全退出
CancellationTokenSource cts = new();

var tcpServer = new TcpServer(new("123", "127.0.0.1", 6688), console);

_ = Task.Factory.StartNew(async () => await tcpServer.TryExecuteAsync());



TcpClientFactory factory = new(console, new("default", "127.0.0.1", 6688));

ITcpClient dataClient = factory.CreateTcpClient();

dataClient.OnMessage += (client, data) =>
{
    Console.WriteLine($"OnMessage: {data}");
};
dataClient.OnSuccess += (client) =>
{
    Console.WriteLine($"OnSuccess");
};

await dataClient.TryExecuteAsync();

Console.ReadLine();
cts.Cancel();
