using Asprtu.Rtu.Contracts;
using Asprtu.Rtu.Contracts.Tcp;

namespace Asprtu.Rtu.TcpClient.Contracts;

/// <summary>
/// Defines the contract for a TCP client that implements protocol communication capabilities.
/// </summary>
public interface ITcpClient : IContracts
{
    /// <summary>
    /// 尝试异步执行TCP客户端操作，通常用于连接操作。
    /// </summary>
    /// <returns>操作结果。</returns>
    Task TryExecuteAsync();

    /// <summary>
    /// 尝试异步写入字节数组到TCP客户端。
    /// </summary>
    /// <param name="bytes">要发送的字节数组。</param>
    /// <returns>如果成功写入，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TryWriteAsync(byte[] bytes);

    /// <summary>
    /// 尝试异步发送整数数据到TCP客户端。
    /// </summary>
    /// <param name="data">要发送的整数数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(int data);

    /// <summary>
    /// 尝试异步发送浮点数据到TCP客户端。
    /// </summary>
    /// <param name="data">要发送的浮点数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(float data);

    /// <summary>
    /// 尝试异步发送双精度浮点数据到TCP客户端。
    /// </summary>
    /// <param name="data">要发送的双精度浮点数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(double data);

    /// <summary>
    /// 尝试异步发送布尔数据到TCP客户端。
    /// </summary>
    /// <param name="data">要发送的布尔数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(bool data);

    /// <summary>
    /// 尝试异步发送短整数数据到TCP客户端。
    /// </summary>
    /// <param name="data">要发送的短整数数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(short data);

    /// <summary>
    /// 尝试异步发送长整数数据到TCP客户端。
    /// </summary>
    /// <param name="data">要发送的长整数数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(long data);

    /// <summary>
    /// 尝试异步发送字节数据到TCP客户端。
    /// </summary>
    /// <param name="data">要发送的字节数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(byte data);

    /// <summary>
    /// 尝试异步发送字符数据到TCP客户端。
    /// </summary>
    /// <param name="data">要发送的字符数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(char data);

    /// <summary>
    /// 尝试异步发送十进制数据到TCP客户端。
    /// </summary>
    /// <param name="data">要发送的十进制数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(decimal data);

    /// <summary>
    /// 尝试异步发送字符串数据到TCP客户端。
    /// </summary>
    /// <param name="data">要发送的字符串数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(string data);

    /// <summary>
    /// 尝试异步发送日期时间数据到TCP客户端。
    /// </summary>
    /// <param name="data">要发送的日期时间数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(DateTime data);

    /// <summary>
    /// 尝试异步发送自定义消息类型数据到TCP客户端。
    /// </summary>
    /// <typeparam name="T">消息类型，必须继承自 <see cref="AbstractMessage"/>。</typeparam>
    /// <param name="data">要发送的消息数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync<T>(T data)
         where T : AbstractMessage, new();

    /// <summary>
    /// 获取与当前上下文关联的TCP连接信息
    /// </summary>
    public TcpInfo TcpInfo { get; }

    /// <summary>
    /// 错误回调，处理异常。
    /// </summary>
    Action<Exception>? OnError { get; set; }

    /// <summary>
    /// 连接成功回调，处理成功建立的TCP连接。
    /// </summary>
    Action<System.Net.Sockets.TcpClient>? OnSuccess { set; get; }

    /// <summary>
    /// 消息回调，处理接收到的消息。
    /// </summary>
    Action<System.Net.Sockets.TcpClient, byte[]>? OnMessage { get; set; }
}