using Microsoft.Extensions.DependencyInjection.Extensions;
using RTU.Infrastructures;
using RTU.Infrastructures.Contracts;
using RTU.Infrastructures.Queue;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 扩展 <see cref="IServiceCollection"/> 以注册泛型队列工厂相关服务。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 <see cref="IQueueFactory{T}"/> 及其实现 <see cref="QueueFactory{T}"/>，
    /// 用于创建和管理指定类型 <typeparamref name="T"/> 的队列客户端。
    /// </summary>
    /// <typeparam name="T">队列中处理的数据类型。</typeparam>
    /// <param name="services">要注册服务的 <see cref="IServiceCollection"/> 实例。</param>
    /// <returns>用于链式调用的 <see cref="IServiceCollection"/> 实例。</returns>
    public static IServiceCollection AddQueueFactory<T>(this IServiceCollection services)
    {
        services.TryAddSingleton<IQueueFactory<T>, QueueFactory<T>>();
        return services;
    }

    /// <summary>
    /// 注册 <see cref="IQueueFactory{T}"/> 及其实现 <see cref="QueueFactory{T}"/>，
    /// 并允许通过指定名称初始化队列客户端。
    /// </summary>
    /// <typeparam name="T">队列中处理的数据类型。</typeparam>
    /// <param name="services">要注册服务的 <see cref="IServiceCollection"/> 实例。</param>
    /// <param name="name">队列的自定义名称。</param>
    /// <returns>用于链式调用的 <see cref="IServiceCollection"/> 实例。</returns>
    public static IServiceCollection AddQueueFactory<T>(this IServiceCollection services, string name)
    {
        services.TryAddSingleton<IQueueFactory<T>>(provider =>
            new QueueFactory<T>(name));
        return services;
    }


    /// <summary>
    /// 注册协议清单（Protocol Manifest）服务，将通过反射扫描实现 <see cref="IProtocol"/> 接口的所有类型，
    /// 并以单例方式注入 <see cref="IProtocolManifest"/>。
    /// </summary>
    /// <param name="services">要注册服务的 <see cref="IServiceCollection"/> 实例。</param>
    /// <returns>用于链式调用的 <see cref="IServiceCollection"/> 实例。</returns>
    public static IServiceCollection AddProtocolManifest(this IServiceCollection services)
    {

        Type protocolType = typeof(IProtocol);

        IEnumerable<Type>? protocolTypes = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => protocolType.IsAssignableFrom(type)
                       && !type.IsInterface
                       && !type.IsAbstract);


        foreach (var manifest in protocolTypes ?? [])
        {
            var closedType = typeof(ProtocolManifest<>).MakeGenericType(manifest);
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IProtocolManifest), closedType));
        }
        return services;
    }
}
