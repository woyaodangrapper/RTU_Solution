using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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

    /// <summary>
    /// 注册协议清单（Protocol Manifest）服务。
    /// 
    /// 该方法会扫描当前 AppDomain 中实现 <see cref="IProtocol"/> 接口的所有非抽象类型，
    /// 并对每个类型执行传入的 <paramref name="action"/> 委托。
    /// 
    /// 可用于自定义注册、日志记录、统计等场景。
    /// </summary>
    /// <typeparam name="TBuilder">应用程序构建器类型，通常为 <see cref="IHostApplicationBuilder"/>。</typeparam>
    /// <param name="builder">应用程序构建器实例。</param>
    /// <param name="action">
    /// 针对每个发现的协议类型执行的操作。参数为发现的 <see cref="Type"/>。
    /// 例如：类型注册、输出日志等。
    /// </param>
    /// <returns>用于链式调用的 <paramref name="builder"/> 实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 null 时抛出。</exception>
    public static TBuilder AddProtocolManifest<TBuilder>(this TBuilder builder, Action<Type> action)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(action);

        Type protocolType = typeof(IProtocol);

        IEnumerable<Type>? protocolTypes = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => protocolType.IsAssignableFrom(type)
                       && !type.IsInterface
                       && !type.IsAbstract);


        foreach (var type in protocolTypes)
            action(type);

        return builder;
    }

}
