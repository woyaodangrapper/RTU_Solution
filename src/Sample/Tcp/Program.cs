using RTU.Infrastructures.Contracts;

var console = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Trace);
});
var builder = WebApplication.CreateBuilder(args);


_ = builder.Services.AddQueueFactory<byte[]>("TCP");
_ = builder.Services.AddTcpServerFactory();
_ = builder.Services.AddTcpClientFactory();

_ = builder.Services.AddProtocolManifest();


var executor = builder.Services.BuildServiceProvider().GetRequiredService<IEnumerable<IProtocolManifest>>();

Console.ReadLine();

//int size = Marshal.SizeOf<MessageHeader>();
//Console.WriteLine("hello world");

//var tcpServer = new TcpServer(new("123", "127.0.0.1", 6688), console);

//_ = Task.Run(async () => await tcpServer.TryExecuteAsync());


//TcpClientFactory factory = new(console, new("default", "127.0.0.1", 6688));

//ITcpClient dataClient = factory.CreateTcpClient();

//dataClient.OnMessage += (client, data) =>
//{
//    Console.WriteLine($"OnMessage: " + ByteConverter.GetObject<string>(data));


//    //data.WriteAsTable();
//};
//dataClient.OnSuccess += (client) =>
//{
//    Console.WriteLine($"OnSuccess");
//};

//_ = Task.Run(async () => await dataClient.TryExecuteAsync());


//// 获取当前程序集中的所有类型
//var assembly = Assembly.GetExecutingAssembly();
//var types = assembly.GetTypes();

//// 筛选出实现了 IProtocol 接口且非抽象类的类型
//var protocolStack = types
//    .Where(t => typeof(IProtocol).IsAssignableFrom(t) && !t.IsAbstract)
//    .ToList();


//Console.ReadLine();
//tcpServer.TrySendAsync(JsonSerializer.Serialize(new { name = "111" }));
//Console.ReadLine();
//tcpServer.TrySendAsync("123465");
//Console.ReadLine();
