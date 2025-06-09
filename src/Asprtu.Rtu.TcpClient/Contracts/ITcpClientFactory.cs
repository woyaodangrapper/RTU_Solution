using Asprtu.Rtu.Contracts;

namespace Asprtu.Rtu.TcpClient.Contracts;

/// <summary>
/// �����ӿڣ����ڴ��� TCP �ͻ��ˡ�
/// </summary>
public interface ITcpClientFactory : ILibraryFactory<TcpClient>
{
    /// <summary>
    /// ����һ�� TCP �ͻ���ʵ�������ڴ���ͻ�������
    /// </summary>
    ITcpClient CreateTcpClient(ChannelOptions options);

    /// <summary>
    /// ʹ��ָ�����ƴ���һ�� TCP �ͻ��˹�������
    /// </summary>
    CreateBuilder CreateBuilder(string name);
}