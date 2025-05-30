using Asprtu.Rtu.Queue;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
}