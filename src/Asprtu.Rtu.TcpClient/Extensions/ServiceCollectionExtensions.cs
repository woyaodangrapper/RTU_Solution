using Asprtu.Rtu.TcpClient;
using Asprtu.Rtu.TcpClient.Contracts;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// ��չ <see cref="IServiceCollection"/> ��ע�᷺�Ͷ��й�����ط���
/// </summary>
public static class ServiceCollectionExtensions
{    /// <summary>
     /// ע������ķ����Դ����͹��� TCP ��������
     /// ʹ�� <see cref="TcpClientFactory"/> �����ʺͲ��� TCP ��������
     /// </summary>
    public static IServiceCollection AddTcpClientFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<ITcpClientFactory, TcpClientFactory>();
        return services;
    }
}