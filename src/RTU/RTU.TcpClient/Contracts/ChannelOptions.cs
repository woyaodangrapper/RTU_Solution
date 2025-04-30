using RTU.Infrastructures.Extensions.Tcp;
using System.Net;

namespace RTU.TcpClient.Contracts;

/// <summary> The options to create a queue. </summary>
public sealed class ChannelOptions
{
    /// <summary>
    /// 初始化 <see cref="ChannelOptions"/> 类的新实例。
    /// </summary>
    /// <param name="channelName">通道名称。</param>
    public ChannelOptions(string channelName)
    {
        ChannelName = channelName;
        IPAddress = IPAddressExtensions.GetLocalIPAddress() ?? throw new Exception("無法獲取本機IP，進行TCP服務代理失敗！");
        Port = IPAddressExtensions.GenerateRandomPort();
    }

    /// <summary>
    /// 初始化 <see cref="ChannelOptions"/> 类的新实例。
    /// </summary>
    /// <param name="channelName">通道名称。</param>
    /// <param name="ip">绑定的 IP 地址。</param>
    /// <param name="port">绑定的端口号。</param>
    public ChannelOptions(string channelName, string ip, int port)
    {
        ChannelName = channelName;
        IPAddress = IPAddress.Parse(ip);
        Port = port;
    }

    /// <summary>
    /// 获取通道唯一名称。
    /// </summary>
    public string ChannelName { get; }

    /// <summary>
    ///绑定的端口号。
    /// </summary>
    public int Port { get; }

    /// <summary>
    ///获取缓冲区的大小（以字节为单位）。这不包括队列标头所需的空间。。
    /// </summary>
    public int Capacity { get; } = 1024; // 10MB

    /// <summary>
    /// 绑定的 IP 地址。
    /// </summary>
    public IPAddress IPAddress { get; }

}