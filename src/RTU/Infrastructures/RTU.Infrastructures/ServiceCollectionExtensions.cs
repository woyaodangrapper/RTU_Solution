using Microsoft.Extensions.DependencyInjection.Extensions;
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
    /// ע��Э���嵥��Protocol Manifest������
    /// 
    /// ��ɨ�赱ǰ AppDomain ��ʵ�� <see cref="IProtocol"/> �ӿڵ����зǳ������ͣ�
    /// ��Ϊÿ��Э�����ʹ�����Ӧ�� <see cref="ProtocolManifest{T}"/> ������
    /// ע��Ϊ <see cref="IProtocolManifest"/>��
    /// </summary>
    /// <param name="services">Ҫע������ <see cref="IServiceCollection"/> ʵ����</param>
    /// <returns>������ʽ���õ� <see cref="IServiceCollection"/> ʵ����</returns>
    public static IServiceCollection AddProtocolManifest(this IServiceCollection services)
    {
        RTU.Infrastructures.Extensions.Util.GetProtocolList(type =>
        {
            var closedType = typeof(ProtocolManifest<>).MakeGenericType(type);
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IProtocolManifest), closedType));
        });

        return services;
    }

}
