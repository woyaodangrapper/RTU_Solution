using Asprtu.Rtu;
using Asprtu.Rtu.Contracts;
using Asprtu.Rtu.TcpClient;
using Asprtu.Rtu.TcpServer;
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
//builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(Asprtu.Rtu.Contracts.ILibraryCapacities), typeof(Asprtu.Rtu.LibraryCapacities<>).MakeGenericType(typeof(global::Asprtu.Rtu.TcpServer.TcpServer))));
//builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(Asprtu.Rtu.Contracts.ILibraryCapacities), typeof(Asprtu.Rtu.LibraryCapacities<>).MakeGenericType(typeof(global::Asprtu.Rtu.TcpClient.TcpClient))));
//builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(Asprtu.Rtu.Contracts.ILibraryCapacities), typeof(Asprtu.Rtu.LibraryCapacities<>).MakeGenericType(typeof(global::Asprtu.Rtu.TcpClient.TcpClient))));
//builder.Services.Add(new ServiceDescriptor(
//         typeof(ILibraryCapacities<TcpClient>),
//         typeof(LibraryCapacities<TcpClient>),
//         ServiceLifetime.Singleton));

//builder.Services.Add(new ServiceDescriptor(
//         typeof(ILibraryCapacities<TcpServer>),
//         typeof(LibraryCapacities<TcpServer>),
//         ServiceLifetime.Singleton));

{
    builder.Services.AddSingleton<ILibraryCapacities<TcpClient>, LibraryCapacities<TcpClient>>();
    builder.Services.AddSingleton<ILibraryCapacities>(sp => sp.GetRequiredService<ILibraryCapacities<TcpClient>>());

    builder.Services.AddSingleton<ILibraryCapacities<TcpServer>, LibraryCapacities<TcpServer>>();
    builder.Services.AddSingleton<ILibraryCapacities>(sp => sp.GetRequiredService<ILibraryCapacities<TcpServer>>());
}
{
    builder.Services.AddSingleton<ILibraryFactory<TcpClient>, TcpClientFactory>();
    builder.Services.AddSingleton<ILibraryCapacitiesFactory<TcpClient>, LibraryCapacitiesFactory<TcpClient>>();

    builder.Services.AddSingleton<ILibraryFactory<TcpServer>, TcpServerFactory>();
    builder.Services.AddSingleton<ILibraryCapacitiesFactory<TcpServer>, LibraryCapacitiesFactory<TcpServer>>();
}
WebApplication app = builder.Build();
using IServiceScope scope = app.Services.CreateScope();

{
    IEnumerable<ILibraryCapacities> definitions = scope.ServiceProvider.GetServices<ILibraryCapacities>();
    var TcpClients = scope.ServiceProvider.GetServices<ILibraryCapacities<TcpClient>>();
}

{
    var definitions = scope.ServiceProvider.GetService<ILibraryCapacitiesFactory<TcpClient>>();

    definitions?.Add("default");
}

Console.ReadLine();

//TcpClientFactory factory = new(console, new("default", "127.0.0.1", 6688));

//ITcpClient dataClient = factory.CreateTcpClient();