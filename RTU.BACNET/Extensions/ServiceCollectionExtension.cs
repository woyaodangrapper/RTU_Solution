using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Net.BACnet;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddBACnetClient(
       this IServiceCollection services,
       Action<BACnetClientOptions>? configureOptions)
    {

        services.Configure(configureOptions ?? (_ => { }));

        // 注册 BACnetClient，并注入 BACnetClientOptions
        services.TryAddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<BACnetClientOptions>>().Value;
            return new BACnetClient(new BACnetIpUdpProtocolTransport(options.Port));
        });

        return services;
    }
}

public class BACnetClientOptions
{
    public int Port { get; set; } = BACnetClient.DEFAULT_UDP_PORT;
    public int Timeout { get; set; } = BACnetClient.DEFAULT_TIMEOUT;
    public int Retries { get; set; } = BACnetClient.DEFAULT_RETRIES;
}
