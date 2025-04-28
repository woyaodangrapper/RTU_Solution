using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RTU.TCPServer.Contracts;

namespace RTU.TCPServer.Extensions;


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