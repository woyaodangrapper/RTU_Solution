using Asprtu.Rtu.Contracts;
using Asprtu.Rtu.Contracts.Tcp;
using System.Net.Sockets;

namespace Asprtu.Rtu.TcpServer.Contracts;

/// <summary>
/// Defines the contract for a TCP server that implements protocol communication capabilities.
/// </summary>
public interface ITcpServer : IContracts
{
    /// <summary>
    /// 尝试启动一个TCP服务器，监听指定的端口。
    /// </summary>
    Task TryExecuteAsync();

    /// <summary>
    /// 尝试异步写入字节数组到 TCP 连接。
    /// </summary>
    /// <param name="bytes">要发送的字节数组。</param>
    /// <param name="client">可选的 TCP 客户端实例，默认为 null。</param>
    /// <returns>如果成功写入数据，返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> TryWriteAsync(byte[] bytes, TcpClient? client = null);

    /// <summary>
    /// 尝试异步发送整数数据到 TCP 连接。
    /// </summary>
    /// <param name="data">要发送的整数数据。</param>
    /// <param name="client">可选的 TCP 客户端实例，默认为 null。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(int data, TcpClient? client = null);

    /// <summary>
    /// 尝试异步发送浮点数据到 TCP 连接。
    /// </summary>
    /// <param name="data">要发送的浮点数据。</param>
    /// <param name="client">可选的 TCP 客户端实例，默认为 null。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(float data, TcpClient? client = null);

    /// <summary>
    /// 尝试异步发送双精度浮点数据到 TCP 连接。
    /// </summary>
    /// <param name="data">要发送的双精度浮点数据。</param>
    /// <param name="client">可选的 TCP 客户端实例，默认为 null。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(double data, TcpClient? client = null);

    /// <summary>
    /// 尝试异步发送布尔数据到 TCP 连接。
    /// </summary>
    /// <param name="data">要发送的布尔数据。</param>
    /// <param name="client">可选的 TCP 客户端实例，默认为 null。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(bool data, TcpClient? client = null);

    /// <summary>
    /// 尝试异步发送短整数数据到 TCP 连接。
    /// </summary>
    /// <param name="data">要发送的短整数数据。</param>
    /// <param name="client">可选的 TCP 客户端实例，默认为 null。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(short data, TcpClient? client = null);

    /// <summary>
    /// 尝试异步发送长整数数据到 TCP 连接。
    /// </summary>
    /// <param name="data">要发送的长整数数据。</param>
    /// <param name="client">可选的 TCP 客户端实例，默认为 null。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(long data, TcpClient? client = null);

    /// <summary>
    /// 尝试异步发送字节数据到 TCP 连接。
    /// </summary>
    /// <param name="data">要发送的字节数据。</param>
    /// <param name="client">可选的 TCP 客户端实例，默认为 null。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(byte data, TcpClient? client = null);

    /// <summary>
    /// 尝试异步发送字符数据到 TCP 连接。
    /// </summary>
    /// <param name="data">要发送的字符数据。</param>
    /// <param name="client">可选的 TCP 客户端实例，默认为 null。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(char data, TcpClient? client = null);

    /// <summary>
    /// 尝试异步发送十进制数据到 TCP 连接。
    /// </summary>
    /// <param name="data">要发送的十进制数据。</param>
    /// <param name="client">可选的 TCP 客户端实例，默认为 null。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(decimal data, TcpClient? client = null);

    /// <summary>
    /// 尝试异步发送字符串数据到 TCP 连接。
    /// </summary>
    /// <param name="data">要发送的字符串数据。</param>
    /// <param name="client">可选的 TCP 客户端实例，默认为 null。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(string data, TcpClient? client = null);

    /// <summary>
    /// 尝试异步发送日期时间数据到 TCP 连接。
    /// </summary>
    /// <param name="data">要发送的日期时间数据。</param>
    /// <param name="client">可选的 TCP 客户端实例，默认为 null。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(DateTime data, TcpClient? client = null);

    /// <summary>
    /// 尝试异步发送自定义消息类型数据到 TCP 连接。
    /// </summary>
    /// <typeparam name="T">消息类型，必须继承自 <see cref="AbstractMessage"/>。</typeparam>
    /// <param name="data">要发送的消息数据。</param>
    /// <param name="client">可选的 TCP 客户端实例，默认为 null。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync<T>(T data, TcpClient? client = null)
         where T : AbstractMessage, new();

    /// <summary>
    /// 获取与当前上下文关联的TCP连接信息。
    /// </summary>
    public TcpInfo TcpInfo { get; }

    /// <summary>
    /// 错误回调事件，处理异常。
    /// </summary>
    Action<Exception>? OnError { get; set; }

    /// <summary>
    /// 成功回调事件，处理成功建立的 TCP 连接。
    /// </summary>
    Action<TcpListener>? OnSuccess { set; get; }

    /// <summary>
    /// 消息回调事件，处理接收到的 TCP 消息。
    /// </summary>
    Action<TcpListener, TcpClient, byte[]>? OnMessage { get; set; }
}