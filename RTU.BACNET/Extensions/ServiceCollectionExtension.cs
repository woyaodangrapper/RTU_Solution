using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.BACnet;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddBACnetClient(this IServiceCollection services, Action<BACnetClientOptions> configureOptions)
    {
        // 配置 BACnetClientOptions
        services.Configure(configureOptions);

        // 注册 BACnetClient，并将其依赖项注入
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<BACnetClientOptions>>().Value;
            var logger = provider.GetRequiredService<ILogger<BACnetClient>>();
            var transport = new BACnetIpUdpProtocolTransport(options.Port);
            return new BACnetClient(transport, options.Timeout, options.Retries, logger);
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
