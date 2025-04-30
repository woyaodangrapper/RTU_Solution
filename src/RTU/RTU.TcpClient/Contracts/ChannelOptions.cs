using RTU.Infrastructures.Extensions.Tcp;
using System.Net;

namespace RTU.TcpClient.Contracts;

/// <summary> The options to create a queue. </summary>
public sealed class ChannelOptions
{
    /// <summary>
    /// ��ʼ�� <see cref="ChannelOptions"/> �����ʵ����
    /// </summary>
    /// <param name="channelName">ͨ�����ơ�</param>
    public ChannelOptions(string channelName)
    {
        ChannelName = channelName;
        IPAddress = IPAddressExtensions.GetLocalIPAddress() ?? throw new Exception("�o���@ȡ���CIP���M��TCP���մ���ʧ����");
        Port = IPAddressExtensions.GenerateRandomPort();
    }

    /// <summary>
    /// ��ʼ�� <see cref="ChannelOptions"/> �����ʵ����
    /// </summary>
    /// <param name="channelName">ͨ�����ơ�</param>
    /// <param name="ip">�󶨵� IP ��ַ��</param>
    /// <param name="port">�󶨵Ķ˿ںš�</param>
    public ChannelOptions(string channelName, string ip, int port)
    {
        ChannelName = channelName;
        IPAddress = IPAddress.Parse(ip);
        Port = port;
    }

    /// <summary>
    /// ��ȡͨ��Ψһ���ơ�
    /// </summary>
    public string ChannelName { get; }

    /// <summary>
    ///�󶨵Ķ˿ںš�
    /// </summary>
    public int Port { get; }

    /// <summary>
    ///��ȡ�������Ĵ�С�����ֽ�Ϊ��λ�����ⲻ�������б�ͷ����Ŀռ䡣��
    /// </summary>
    public int Capacity { get; } = 1024; // 10MB

    /// <summary>
    /// �󶨵� IP ��ַ��
    /// </summary>
    public IPAddress IPAddress { get; }

}