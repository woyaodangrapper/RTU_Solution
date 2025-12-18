using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection;

public static class LibraryOptionExtensions
{
    public static IHostApplicationBuilder AddLibraryOptions([NotNull] this IHostApplicationBuilder builder)
    {
        _ = builder.Services
             .AddSingleton(sp => new Aspdcs.Rtu.TcpServer.Contracts.ChannelOptions("server", "0.0.0.0", 1868))
             .AddSingleton(sp => new Aspdcs.Rtu.TcpClient.Contracts.ChannelOptions("client", "0.0.0.0", 1868));

        return builder;
    }
}