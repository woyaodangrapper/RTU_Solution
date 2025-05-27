namespace Asprtu.Rtu.TcpClient.Contracts;

/// <summary>
/// �����ӿڣ����ڴ��� TCP �ͻ��ˡ�
/// </summary>
public interface ITcpClientFactory
{
    /// <summary>
    /// ����һ�� TCP �ͻ���ʵ�������ڴ���ͻ�������
    /// </summary>
    ITcpClient CreateTcpClient(ChannelOptions options);
}
