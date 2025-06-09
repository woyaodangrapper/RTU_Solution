using Asprtu.Rtu.Extensions.Tcp;
using System.Net;

namespace Asprtu.Rtu.TcpServer.Contracts;

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
        IPAddress = new IPAddress([0, 0, 0, 0]);//IPAddressExtensions.GetLocalIPAddress() ?? throw new InvalidOperationException("�o���@ȡ���CIP���M��TCP���մ���ʧ����");
        Port = IPAddressExtensions.GenerateRandomPort(1868);
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

    public ChannelOptions(string channelName, IPAddress ip, int port, int capacity)
    {
        ChannelName = channelName;
        IPAddress = ip;
        Port = port;
        Capacity = capacity;
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
    ///��ȡ�������Ĵ�С�����ֽ�Ϊ��λ�����ⲻ�������б�ͷ����Ŀռ䡣����
    /// </summary>
    public int Capacity { get; } = 1024; // 10MB

    /// <summary>
    /// �󶨵� IP ��ַ��
    /// </summary>
    public IPAddress IPAddress { get; }
}

public class CreateBuilder(string channelName)
{
    private string _channelName = channelName;
    private IPAddress _ipAddress = new([0, 0, 0, 0]);
    private int _port = IPAddressExtensions.GenerateRandomPort(1868);
    private int _capacity = 1024;

    public CreateBuilder SetAddress(IPAddress ip)
    {
        _ipAddress = ip;
        return this;
    }

    public CreateBuilder SetPort(int port)
    {
        _port = port;
        return this;
    }

    public CreateBuilder SetCapacity(int capacity)
    {
        _capacity = capacity;
        return this;
    }

    public ChannelOptions Build() => new ChannelOptions(_channelName, _ipAddress, _port, _capacity);
}