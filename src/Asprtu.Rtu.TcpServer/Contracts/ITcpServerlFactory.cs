using Asprtu.Rtu.Contracts;

namespace Asprtu.Rtu.TcpServer.Contracts;

/// <summary>
/// �����ӿڣ����ڴ��� TCP ��������
/// </summary>
public interface ITcpServerFactory : ILibraryFactory<TcpServer>

{
    /// <summary>
    /// ����һ�� TCP ������ʵ�������ڴ���ͻ�������
    /// </summary>
    ITcpServer CreateTcpServer(ChannelOptions options);
}