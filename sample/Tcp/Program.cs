using Aspdcs.Rtu;
using Aspdcs.Rtu.Contracts;
using Aspdcs.Rtu.TcpClient;
using Aspdcs.Rtu.TcpServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var console = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Trace);
});

var builder = WebApplication.CreateSlimBuilder();
//builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(Aspdcs.Rtu.Contracts.ILibraryCapacities), typeof(Aspdcs.Rtu.LibraryCapacities<>).MakeGenericType(typeof(global::Aspdcs.Rtu.TcpServer.TcpServer))));
//builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(Aspdcs.Rtu.Contracts.ILibraryCapacities), typeof(Aspdcs.Rtu.LibraryCapacities<>).MakeGenericType(typeof(global::Aspdcs.Rtu.TcpClient.TcpClient))));
//builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(Aspdcs.Rtu.Contracts.ILibraryCapacities), typeof(Aspdcs.Rtu.LibraryCapacities<>).MakeGenericType(typeof(global::Aspdcs.Rtu.TcpClient.TcpClient))));
//builder.Services.Add(new ServiceDescriptor(
//         typeof(ILibraryCapacities<TcpClient>),
//         typeof(LibraryCapacities<TcpClient>),
//         ServiceLifetime.Singleton));

//builder.Services.Add(new ServiceDescriptor(
//         typeof(ILibraryCapacities<TcpServer>),
//         typeof(LibraryCapacities<TcpServer>),
//         ServiceLifetime.Singleton));
builder.AddLibraryOptions();

{
    builder.Services.AddSingleton<ILibraryCapacities<TcpServer>, LibraryCapacities<TcpServer>>();

    //builder.Services.AddSingleton<ILibraryCapacities<TcpClient>>(provider =>
    //{
    //    var options = new Aspdcs.Rtu.TcpClient.Contracts.ChannelOptions("test", "127.0.0.1", 1868);
    //    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
    //    var client = new TcpClient(options, loggerFactory);
    //    return new LibraryCapacities<TcpClient>(client);
    //});

    //builder.Services.AddSingleton<ILibraryCapacities<TcpServer>>(provider =>
    //{
    //    var options = new Aspdcs.Rtu.TcpServer.Contracts.ChannelOptions("test", "127.0.0.1", 1868);
    //    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

    //    var server = new TcpServer(options, loggerFactory);
    //    return new LibraryCapacities<TcpServer>(server);
    //});
    //builder.Services.AddSingleton<ILibraryCapacities>(sp => sp.GetRequiredService<ILibraryCapacities<TcpClient>>());
    //builder.Services.AddSingleton<ILibraryCapacities<ITcpClient>>(sp => sp.GetRequiredService<ILibraryCapacities<TcpClient>>());

    //builder.Services.AddSingleton<ILibraryCapacities>(sp => sp.GetRequiredService<ILibraryCapacities<TcpServer>>());
    //builder.Services.AddSingleton<ILibraryCapacities<ITcpServer>>(sp => sp.GetRequiredService<ILibraryCapacities<TcpServer>>());
}

//{
builder.Services.AddSingleton<ILibraryFactory<TcpClient>, TcpClientFactory>();
//    builder.Services.AddSingleton<ILibraryCapacitiesFactory<TcpClient>, LibraryCapacitiesFactory<TcpClient>>();

//    builder.Services.AddSingleton<ILibraryCapacitiesFactory<ITcpClient>>(sp => sp.GetRequiredService<ILibraryCapacitiesFactory<TcpClient>>());

//builder.Services.AddSingleton<ILibraryFactory<TcpServer>, TcpServerFactory>();
//builder.Services.AddSingleton<ILibraryCapacitiesFactory<TcpServer>, LibraryCapacitiesFactory<TcpServer>>();

//builder.Services.AddSingleton<ILibraryCapacitiesFactory<ITcpServer>>(sp => sp.GetRequiredService<ILibraryCapacitiesFactory<TcpServer>>());
//}
WebApplication app = builder.Build();
using IServiceScope scope = app.Services.CreateScope();

//{
//    IEnumerable<ILibraryCapacities> definitions = scope.ServiceProvider.GetServices<ILibraryCapacities>();
//    var TcpClients = scope.ServiceProvider.GetServices<ILibraryCapacities<TcpClient>>();
//    var ITcpClients = scope.ServiceProvider.GetServices<ILibraryCapacities<ITcpClient>>();

//    var ILibraryCapacitiesFactory = scope.ServiceProvider.GetServices<ILibraryCapacitiesFactory<ITcpClient>>();
//}

//{
//    var definitions = scope.ServiceProvider.GetService<ILibraryCapacitiesFactory<TcpClient>>();

//    definitions?.Add("default");
//}
{
    var a = scope.ServiceProvider.GetService<ILibraryCapacities<TcpServer>>()?.Contracts;
    //if (scope.ServiceProvider.GetService<ILibraryCapacities<ITcpServer>>()?.Contracts is { } _tcpServer)
    //{
    //    _ = _tcpServer.TryExecuteAsync();
    //    Thread.Sleep(1000);
    //    await _tcpServer.TrySendAsync(1);

    //    _tcpServer.OnMessage += (server, client, data) => Console.WriteLine($"Received data from {client.Client.RemoteEndPoint}: {BitConverter.ToInt32(data, 0)}");
    //}

    //if (scope.ServiceProvider.GetService<ILibraryCapacities<ITcpClient>>()?.Contracts is { } _tcpClient)
    //{
    //    _ = _tcpClient.TryExecuteAsync();
    //    Thread.Sleep(1000);
    //    await _tcpClient.TrySendAsync(1);
    //}
}

{
    var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();

    var tcpServerFactory = new TcpServerFactory(loggerFactory);
    var dataServer = tcpServerFactory.CreateTcpServer(new("default", "127.0.0.1", 6688));

    _ = Task.Factory.StartNew(async () => await dataServer.TryExecuteAsync());

    var tcpClientFactory = new TcpClientFactory(loggerFactory);
    var dataClient = tcpClientFactory.CreateTcpClient(new("default", "127.0.0.1", 6688));
    Thread.Sleep(1000); // Ensure server is ready before client tries to connect
    _ = dataClient.TryExecuteAsync();

    List<Task<bool>> tasks = new List<Task<bool>>();
    for (int i = 0; i < 100; i++)
    {
        tasks.Add(dataClient.TrySendAsync(i));
    }
    try
    {
        await Task.WhenAll(tasks);
    }
    catch (Exception)
    {
        throw;
    }

    var bytes = new List<byte[]>();
    dataServer.OnMessage += (server, client, data) =>
    {
        bytes.Add(data);
        Console.WriteLine($"Client connected: {data.Length}");
    };
    Console.ReadLine();

    await dataClient.TrySendAsync(1);
    Console.ReadLine();
}

//TcpClientFactory factory = new(console, new("default", "127.0.0.1", 6688));

//ITcpClient dataClient = factory.CreateTcpClient();