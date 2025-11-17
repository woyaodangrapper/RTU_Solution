using Asprtu.Rtu.Contracts;
using Asprtu.Rtu.Contracts.DLT645;

namespace Asprtu.Rtu.DLT645.Contracts;

/// <summary>
///  定义 DLT645 协议客户端的通信契约。
/// </summary>
public interface IDlt645Client : IContracts
{
    /// <summary>
    /// 获取总线设备地址。
    /// </summary>
    /// <returns>操作结果。</returns>
    Task<IAsyncEnumerable<MessageHeader>> TryReadAddressAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 尝试异步写入字节数组到从站。
    /// </summary>
    /// <param name="bytes">要发送的字节数组。</param>
    /// <returns>如果成功写入，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TryWriteAsync(byte[] bytes);


    /// <summary>
    /// 尝试异步发送字节数据到从站。
    /// </summary>
    /// <param name="data">要发送的字节数据。</param>
    /// <param name="message">从站返回的字节数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync(byte data, out byte[] message);

    /// <summary>
    /// 尝试异步发送自定义消息类型数据到从站。
    /// </summary>
    /// <typeparam name="T">消息类型，必须继承自 <see cref="AbstractMessage"/>。</typeparam>
    /// <param name="data">要发送的消息数据。</param>
    /// <returns>如果成功发送数据，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    Task<bool> TrySendAsync<T>(T data)
         where T : AbstractMessage, new();
}