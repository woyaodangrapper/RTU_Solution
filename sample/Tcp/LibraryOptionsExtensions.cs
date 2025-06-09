using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

internal static class LibraryOptionsExtensions
{
    public static void AddRtuOptions(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton(sp =>
            new Asprtu.Rtu.TcpServer.Contracts.ChannelOptions("server", "0.0.0.0", 502));

        builder.Services.AddSingleton(sp =>
            new Asprtu.Rtu.TcpClient.Contracts.ChannelOptions("client", "0.0.0.0", 502));
    }
}