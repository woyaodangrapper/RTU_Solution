using Microsoft.Extensions.DependencyInjection.Extensions;
using RTU.TcpClient;
using RTU.TcpClient.Contracts;

namespace Microsoft.Extensions.DependencyInjection;


/// <summary>
/// 扩展 <see cref="IServiceCollection"/> 以注册 TCP 客户端相关服务。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册所需的服务以创建和管理 TCP 客户端。
    /// 使用 <see cref="ITcpClientFactory"/> 来访问和操作 TCP 客户端。
    /// </summary>
    public static IServiceCollection AddTcpClientFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<ITcpClientFactory, TcpClientFactory>();
        return services;
    }
}