using Microsoft.Extensions.DependencyInjection.Extensions;
using Asprtu.Rtu.TcpServer;
using Asprtu.Rtu.TcpServer.Contracts;

namespace Microsoft.Extensions.DependencyInjection;


/// <summary>
/// ��չ <see cref="IServiceCollection"/> ��ע�� TCP ��������ط���
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// ע������ķ����Դ����͹��� TCP ��������
    /// ʹ�� <see cref="TcpServerFactory"/> �����ʺͲ��� TCP ��������
    /// </summary>
    public static IServiceCollection AddTcpServerFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<ITcpServerFactory, TcpServerFactory>();
        return services;
    }
}