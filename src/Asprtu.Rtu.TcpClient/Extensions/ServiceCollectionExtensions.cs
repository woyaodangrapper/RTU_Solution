using Asprtu.Rtu.TcpClient;
using Asprtu.Rtu.TcpClient.Contracts;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 扩展 <see cref="IServiceCollection"/> 以注册泛型队列工厂相关服务。
/// </summary>
public static class ServiceCollectionExtensions
{    /// <summary>
     /// 注册所需的服务以创建和管理 TCP 服务器。
     /// 使用 <see cref="TcpClientFactory"/> 来访问和操作 TCP 服务器。
     /// </summary>
    public static IServiceCollection AddTcpClientFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<ITcpClientFactory, TcpClientFactory>();
        return services;
    }
}