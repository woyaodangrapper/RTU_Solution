using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RTU.Infrastructures;
using RTU.Infrastructures.Contracts;
using RTU.Infrastructures.Queue;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// ��չ <see cref="IServiceCollection"/> ��ע�᷺�Ͷ��й�����ط���
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// ע�� <see cref="IQueueFactory{T}"/> ����ʵ�� <see cref="QueueFactory{T}"/>��
    /// ���ڴ����͹���ָ������ <typeparamref name="T"/> �Ķ��пͻ��ˡ�
    /// </summary>
    /// <typeparam name="T">�����д�����������͡�</typeparam>
    /// <param name="services">Ҫע������ <see cref="IServiceCollection"/> ʵ����</param>
    /// <returns>������ʽ���õ� <see cref="IServiceCollection"/> ʵ����</returns>
    public static IServiceCollection AddQueueFactory<T>(this IServiceCollection services)
    {
        services.TryAddSingleton<IQueueFactory<T>, QueueFactory<T>>();
        return services;
    }

    /// <summary>
    /// ע�� <see cref="IQueueFactory{T}"/> ����ʵ�� <see cref="QueueFactory{T}"/>��
    /// ������ͨ��ָ�����Ƴ�ʼ�����пͻ��ˡ�
    /// </summary>
    /// <typeparam name="T">�����д�����������͡�</typeparam>
    /// <param name="services">Ҫע������ <see cref="IServiceCollection"/> ʵ����</param>
    /// <param name="name">���е��Զ������ơ�</param>
    /// <returns>������ʽ���õ� <see cref="IServiceCollection"/> ʵ����</returns>
    public static IServiceCollection AddQueueFactory<T>(this IServiceCollection services, string name)
    {
        services.TryAddSingleton<IQueueFactory<T>>(provider =>
            new QueueFactory<T>(name));
        return services;
    }


    /// <summary>
    /// ע��Э���嵥��Protocol Manifest�����񣬽�ͨ������ɨ��ʵ�� <see cref="IProtocol"/> �ӿڵ��������ͣ�
    /// ���Ե�����ʽע�� <see cref="IProtocolManifest"/>��
    /// </summary>
    /// <param name="services">Ҫע������ <see cref="IServiceCollection"/> ʵ����</param>
    /// <returns>������ʽ���õ� <see cref="IServiceCollection"/> ʵ����</returns>
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
    /// ע��Э���嵥��Protocol Manifest������
    /// 
    /// �÷�����ɨ�赱ǰ AppDomain ��ʵ�� <see cref="IProtocol"/> �ӿڵ����зǳ������ͣ�
    /// ����ÿ������ִ�д���� <paramref name="action"/> ί�С�
    /// 
    /// �������Զ���ע�ᡢ��־��¼��ͳ�Ƶȳ�����
    /// </summary>
    /// <typeparam name="TBuilder">Ӧ�ó��򹹽������ͣ�ͨ��Ϊ <see cref="IHostApplicationBuilder"/>��</typeparam>
    /// <param name="builder">Ӧ�ó��򹹽���ʵ����</param>
    /// <param name="action">
    /// ���ÿ�����ֵ�Э������ִ�еĲ���������Ϊ���ֵ� <see cref="Type"/>��
    /// ���磺����ע�ᡢ�����־�ȡ�
    /// </param>
    /// <returns>������ʽ���õ� <paramref name="builder"/> ʵ����</returns>
    /// <exception cref="ArgumentNullException">�� <paramref name="action"/> Ϊ null ʱ�׳���</exception>
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
