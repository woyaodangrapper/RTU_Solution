using Asprtu.Rtu.DLT645;
using Asprtu.Rtu.DLT645.Contracts;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
}
