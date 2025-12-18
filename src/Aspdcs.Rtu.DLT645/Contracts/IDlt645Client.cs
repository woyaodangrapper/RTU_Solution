using Aspdcs.Rtu.Contracts;
using Aspdcs.Rtu.Contracts.DLT645;
using System.Diagnostics.CodeAnalysis;

namespace Aspdcs.Rtu.DLT645.Contracts;

/// <summary>
/// 定义 DLT645 协议客户端的通用契约。
/// 提供符合国标 DLT645 标准的设备进行通讯的方法，支持发送和接收消息。
/// </summary>
public interface IDlt645Client : IContracts
{
    /// <summary>
    /// 异步发送 DLT645 命令并返回语义值
    /// </summary>
    /// <param name="code">控制码</param>
    /// <param name="addresses">设备地址（6 字节）</param>
    /// <param name="data">可选数据域</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回解析后的语义值序列</returns>
    IAsyncEnumerable<SemanticValue> TrySendAsync(byte code, byte[] addresses, byte[]? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步发送 DLT645 命令并返回指定类型的语义值
    /// </summary>
    /// <typeparam name="T">语义值类型</typeparam>
    /// <param name="code">控制码</param>
    /// <param name="addresses">设备地址（6 字节）</param>
    /// <param name="data">可选数据域</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回解析后的指定类型语义值序列</returns>
    IAsyncEnumerable<T> TrySendAsync<T>(byte code, byte[] addresses, byte[]? data = null, CancellationToken cancellationToken = default)
        where T : SemanticValue;

    /// <summary>
    /// 异步发送 DLT645 消息头并返回指定类型的语义值
    /// </summary>
    /// <typeparam name="T">语义值类型</typeparam>
    /// <param name="messageHeader">消息头</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回解析后的指定类型语义值序列</returns>
    IAsyncEnumerable<T> TrySendAsync<T>(MessageHeader messageHeader, CancellationToken cancellationToken = default)
        where T : SemanticValue;

    /// <summary>
    /// 异步批量发送多个 DLT645 消息头并返回指定类型的语义值
    /// </summary>
    /// <typeparam name="T">语义值类型</typeparam>
    /// <param name="messages">消息头集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回解析后的指定类型语义值序列</returns>
    IAsyncEnumerable<T> TrySendAsync<T>([NotNull] IEnumerable<MessageHeader> messages, CancellationToken cancellationToken = default)
        where T : SemanticValue;

    /// <summary>
    /// 异步发送枚举命令
    /// </summary>
    /// <typeparam name="T">命令枚举类型</typeparam>
    /// <param name="command">枚举命令值</param>
    /// <param name="addresses">设备地址（6 字节）</param>
    /// <param name="data">可选数据域</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> TrySendAsync<T>(T command, byte[] addresses, byte[]? data = null, CancellationToken cancellationToken = default)
        where T : Enum;

    /// <summary>
    /// 异步发送枚举命令
    /// </summary>
    /// <typeparam name="T">命令枚举类型</typeparam>
    /// <param name="command">枚举命令值</param>
    /// <param name="addresses">设备地址（6 字节）</param>
    /// <param name="data">可选数据域</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> TrySendAsync<T>(T command, string addresses, byte[]? data = null, CancellationToken cancellationToken = default)
        where T : Enum;

    /// <summary>
    /// 异步写入原始字节数据
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回消息头序列</returns>
    IAsyncEnumerable<MessageHeader> TryWriteAsync(byte[] bytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步写入原始字节数据（Memory 版本）
    /// </summary>
    /// <param name="buffer">内存缓冲区</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回消息头序列</returns>
    IAsyncEnumerable<MessageHeader> TryWriteAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步读取设备地址（广播方式）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回消息头序列</returns>
    Task<IAsyncEnumerable<MessageHeader>> TryReadAddressAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步写入数据到指定串口
    /// </summary>
    int Write(string comPort, byte[] buffer);
    int Write(string comPort, byte[] buffer, int offset, int count);
    int Write(string comPort, Span<byte> buffer);

    /// <summary>
    /// 同步从指定串口读取数据
    /// </summary>
    int Read(string comPort, byte[] buffer);
    int Read(string comPort, byte[] buffer, int offset, int count);
    int Read(string comPort, Span<byte> buffer);

    /// <summary>
    /// 异步写入数据到指定串口
    /// </summary>
    Task<int> WriteAsync(string comPort, byte[] buffer, CancellationToken cancellationToken = default);
    Task<int> WriteAsync(string comPort, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);
    Task<int> WriteAsync(string comPort, Memory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步从指定串口读取数据
    /// </summary>
    Task<int> ReadAsync(string comPort, byte[] buffer, CancellationToken cancellationToken = default);
    Task<int> ReadAsync(string comPort, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);
    Task<int> ReadAsync(string comPort, Memory<byte> buffer, CancellationToken cancellationToken = default);
}