namespace Asprtu.Rtu.TcpServer.Contracts;

/// <summary>
/// �����ӿڣ����ڴ��� TCP ��������
/// </summary>
public interface ITcpServerFactory
{
    /// <summary>
    /// ����һ�� TCP ������ʵ�������ڴ���ͻ�������
    /// </summary>
    ITcpServer CreateTcpServer(ChannelOptions options);
}
