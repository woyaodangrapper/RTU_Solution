using Microsoft.Extensions.DependencyInjection.Extensions;
using RTU.TcpClient;
using RTU.TcpClient.Contracts;

namespace Microsoft.Extensions.DependencyInjection;


/// <summary>
/// ��չ <see cref="IServiceCollection"/> ��ע�� TCP �ͻ�����ط���
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// ע������ķ����Դ����͹��� TCP �ͻ��ˡ�
    /// ʹ�� <see cref="ITcpClientFactory"/> �����ʺͲ��� TCP �ͻ��ˡ�
    /// </summary>
    public static IServiceCollection AddTcpClientFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<ITcpClientFactory, TcpClientFactory>();
        return services;
    }
}