using Aspdcs.Rtu.DLT645;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ThrowHelper = Aspdcs.Rtu.Extensions.ThrowHelper;
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 扩展 <see cref="IServiceCollection"/> 以注册 DLT645 客户端相关服务
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册单例的服务以创建和管理 DLT645 客户端。
    /// 使用 <see cref="Dlt645ClientFactory"/> 来创建和操作 DLT645 客户端。
    /// </summary>
    public static IServiceCollection AddDlt645ClientFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<IDlt645ClientFactory, Dlt645ClientFactory>();
        return services;
    }



    /// <summary>
    /// 注册配置好的 DLT645 客户端实例作为单例服务。
    /// 该方法会自动注册 <see cref="IDlt645ClientFactory"/> 并使用它创建客户端实例。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">用于配置 <see cref="ChannelOptions"/> 的委托</param>
    /// <returns>服务集合，以便链式调用</returns>
    public static IServiceCollection AddDlt645Client(this IServiceCollection services, Action<ChannelOptions> configureOptions)
    {
        ThrowHelper.ThrowIfNull(configureOptions);

        // 确保工厂已注册
        services.AddDlt645ClientFactory();

        // 注册配置好的客户端实例
        services.TryAddSingleton<IDlt645Client>(serviceProvider =>
        {
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var options = new ChannelOptions("default");
            configureOptions(options);
            return new Dlt645Client(options, loggerFactory ?? NullLoggerFactory.Instance);
        });

        return services;
    }


    /// <summary>
    /// 注册配置好的 DLT645 客户端实例作为单例服务（通过 CreateBuilder 模式）。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="name">客户端名称</param>
    /// <param name="configureBuilder">用于配置 <see cref="CreateBuilder"/> 的委托</param>
    /// <returns>服务集合，以便链式调用</returns>
    public static IServiceCollection AddDlt645Client(this IServiceCollection services, string name, Action<ChannelOptions.Builder> configureBuilder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("客户端名称不能为空", nameof(name));

        ThrowHelper.ThrowIfNull(configureBuilder);

        services.TryAddSingleton<IDlt645Client>(serviceProvider =>
        {
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            var builder = new ChannelOptions.Builder(name);

            if (loggerFactory != null)
            {
                builder.WithLogger(loggerFactory);
            }

            configureBuilder(builder);

            return builder.Run();
        });

        return services;
    }

    /// <summary>
    /// 注册配置好的 DLT645 客户端实例作为单例服务（直接使用 ChannelOptions）。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="options">预先配置好的通道选项</param>
    /// <returns>服务集合，以便链式调用</returns>
    public static IServiceCollection AddDlt645Client(this IServiceCollection services, ChannelOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        services.TryAddSingleton<IDlt645Client>(serviceProvider =>
        {
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            return new Dlt645Client(options, loggerFactory ?? NullLoggerFactory.Instance);
        });

        return services;
    }

    /// <summary>
    /// 注册配置好的 DLT645 客户端实例作为单例服务（使用简化的串口字符串配置）。
    /// 如 "COM5"
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="comStr">串口配置字符串</param>
    /// <returns>服务集合，以便链式调用</returns>
    public static IServiceCollection AddDlt645Client(this IServiceCollection services, string comStr)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(comStr);

        services.TryAddSingleton<IDlt645Client>(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ILoggerFactory>();

            return new ChannelOptions.Builder("default")
                .WithChannel(comStr)
                .WithLogger(logger)
                .Run();
        });

        return services;
    }

}