using Microsoft.Extensions.DependencyInjection.Extensions;
using Aspdcs.Rtu.TcpServer;
using Aspdcs.Rtu.TcpServer.Contracts;

namespace Microsoft.Extensions.DependencyInjection;


/// <summary>
/// 扩展 <see cref="IServiceCollection"/> 以注册 TCP 服务器相关服务。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册所需的服务以创建和管理 TCP 服务器。
    /// 使用 <see cref="TcpServerFactory"/> 来访问和操作 TCP 服务器。
    /// </summary>
    public static IServiceCollection AddTcpServerFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<ITcpServerFactory, TcpServerFactory>();
        return services;
    }
}