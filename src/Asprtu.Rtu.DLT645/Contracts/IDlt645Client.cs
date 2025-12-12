using Asprtu.Rtu.Contracts;
using Asprtu.Rtu.Contracts.DLT645;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Asprtu.Rtu.DLT645.Contracts;

/// <summary>
/// 定义 DLT645 协议客户端的通信契约。
/// 提供用于与符合 DLT645 标准的设备进行通信的方法，支持发送和接收消息。
/// </summary>
public interface IDlt645Client : IContracts
{

    /// <summary>
    /// 异步发送 DLT645 命令到指定地址列表。
    /// </summary>
    /// <typeparam name="T">命令枚举类型。</typeparam>
    /// <param name="command">要发送的命令值。</param>
    /// <param name="addresses">电表地址字符串，可包含多个地址。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>返回每次发送结果的异步枚举序列。</returns>
    IAsyncEnumerable<MessageHeader> TrySendAsync<T>(T command, string addresses, CancellationToken cancellationToken);


    /// <summary>
    /// 异步顺序发送一组 DLT645 消息帧。
    /// </summary>
    /// <param name="messages">要发送的消息帧集合。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>返回每条消息发送结果的异步枚举序列。</returns>
    IAsyncEnumerable<MessageHeader> TrySendAsync([NotNull] IEnumerable<MessageHeader> messages, CancellationToken cancellationToken);

    /// <summary>
    /// 异步发送 DLT645 命令到指定地址。
    /// </summary>
    /// <param name="code">控制码，指定命令类型。</param>
    /// <param name="addresses">目标设备地址（6 字节）。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>返回接收到的响应消息头的异步枚举序列。</returns>
    IAsyncEnumerable<MessageHeader> TrySendAsync(byte code, byte[] addresses, CancellationToken cancellationToken);

    /// <summary>
    /// 异步发送 DLT645 消息。
    /// </summary>
    /// <param name="message">要发送的完整消息头。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>返回接收到的响应消息头的异步枚举序列。</returns>
    IAsyncEnumerable<MessageHeader> TrySendAsync(MessageHeader message, CancellationToken cancellationToken);

    /// <summary>
    /// 异步发送泛型抽象消息对象。
    /// </summary>
    /// <typeparam name="T">继承自 <see cref="AbstractMessage"/> 的消息类型。</typeparam>
    /// <param name="data">要发送的消息对象。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>返回接收到的响应消息头的异步枚举序列。</returns>
    IAsyncEnumerable<MessageHeader> TrySendAsync<T>([NotNull] T data, CancellationToken cancellationToken)
           where T : AbstractMessage, new();

    /// <summary>
    /// 异步写入原始字节数据到串口。
    /// </summary>
    /// <param name="bytes">要发送的字节数组。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>返回接收到的响应消息头的异步枚举序列。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IAsyncEnumerable<MessageHeader> TryWriteAsync(byte[] bytes, CancellationToken cancellationToken);
}