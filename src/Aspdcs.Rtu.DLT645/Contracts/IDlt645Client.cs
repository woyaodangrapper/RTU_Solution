using Aspdcs.Rtu.Contracts;
using Aspdcs.Rtu.Contracts.DLT645;
using System.Diagnostics.CodeAnalysis;

namespace Aspdcs.Rtu.DLT645;

/// <summary>
/// 定义 DLT645 协议客户端的通用契约。
/// 提供符合国标 DLT645 标准的设备进行通讯的方法，支持发送和接收消息。
/// </summary>
public interface IDlt645Client : IContracts
{

    /// <summary>
    /// 写入原始字节数据
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回消息头序列</returns>
    IAsyncEnumerable<MessageHeader> TryWriteAsync(byte[] bytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入原始字节数据（Memory 版本）
    /// </summary>
    /// <param name="buffer">内存缓冲区</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回消息头序列</returns>
    IAsyncEnumerable<MessageHeader> TryWriteAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取设备地址（广播方式）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回消息头序列</returns>
    IAsyncEnumerable<AddressValue> TryReadAddressAsync(CancellationToken cancellationToken = default);

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
    /// 写入数据到指定串口
    /// </summary>
    Task<int> WriteAsync(string comPort, byte[] buffer, CancellationToken cancellationToken = default);
    Task<int> WriteAsync(string comPort, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);
    Task<int> WriteAsync(string comPort, Memory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从指定串口读取数据
    /// </summary>
    Task<int> ReadAsync(string comPort, byte[] buffer, CancellationToken cancellationToken = default);
    Task<int> ReadAsync(string comPort, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);
    Task<int> ReadAsync(string comPort, Memory<byte> buffer, CancellationToken cancellationToken = default);


    /// <summary>
    /// 读取设备数据总电能
    /// </summary>
    /// <param name="address">设备地址</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> ReadAsync(string address, CancellationToken ct = default);

    /// <summary>
    /// 读取设备数据总电能
    /// </summary>
    /// <param name="addresses">设备地址</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> ReadAsync([NotNull] IEnumerable<AddressValue> addresses, CancellationToken ct = default);

    /// <summary>
    /// 读取设备数据
    /// </summary>
    /// <param name="addresses">设备地址</param>
    /// <param name="dataId">数据标识</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> ReadAsync([NotNull] IEnumerable<AddressValue> addresses, uint dataId, CancellationToken ct = default);

    /// <summary>
    /// 读取设备数据（字节数组地址）
    /// </summary>
    /// <param name="address">设备地址</param>
    /// <param name="dataId">数据标识</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> ReadAsync(byte[] address, uint dataId, CancellationToken ct = default);

    /// <summary>
    /// 读取设备数据（字符串地址）
    /// </summary>
    /// <param name="address">设备地址字符串</param>
    /// <param name="dataId">数据标识</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> ReadAsync(string address, uint dataId, CancellationToken ct = default);

    /// <summary>
    /// 读取设备数据（使用自定义命令码，字节数组地址）
    /// </summary>
    /// <param name="command">命令码</param>
    /// <param name="address">设备地址</param>
    /// <param name="dataId">数据标识</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> ReadAsync(uint command, byte[] address, uint dataId, CancellationToken ct = default);

    /// <summary>
    /// 读取设备数据（使用自定义命令码，字符串地址，支持多地址）
    /// </summary>
    /// <param name="command">命令码</param>
    /// <param name="addresses">设备地址字符串（支持格式化多地址）</param>
    /// <param name="dataId">数据标识</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> ReadAsync(uint command, string addresses, uint dataId, CancellationToken ct = default);

    /// <summary>
    /// 读取后续帧数据
    /// </summary>
    /// <param name="address">设备地址</param>
    /// <param name="frameIndex">帧序号</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> ReadNextAsync(byte[] address, byte frameIndex, CancellationToken ct = default);


    /// <summary>
    /// 读取后续帧数据
    /// </summary>
    /// <param name="addresses">设备地址</param>
    /// <param name="frameIndex">帧序号</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> ReadNextAsync(string addresses, byte frameIndex, CancellationToken ct = default);

    /// <summary>
    /// 写入设备数据（字节数组地址）
    /// </summary>
    /// <param name="address">设备地址</param>
    /// <param name="dataId">数据标识</param>
    /// <param name="password">密码</param>
    /// <param name="operatorCode">操作者代码</param>
    /// <param name="payload">有效载荷数据</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> WriteAsync(byte[] address, uint dataId, uint password, uint operatorCode, ReadOnlySpan<byte> payload, CancellationToken ct = default);

    /// <summary>
    /// 写入设备数据（使用自定义命令码，字节数组地址）
    /// </summary>
    /// <param name="command">命令码</param>
    /// <param name="address">设备地址</param>
    /// <param name="dataId">数据标识</param>
    /// <param name="password">密码</param>
    /// <param name="operatorCode">操作者代码</param>
    /// <param name="payload">有效载荷数据</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> WriteAsync(uint command, byte[] address, uint dataId, uint password, uint operatorCode, ReadOnlySpan<byte> payload, CancellationToken ct = default);

    /// <summary>
    /// 写入设备数据（字符串地址）
    /// </summary>
    /// <param name="address">设备地址字符串</param>
    /// <param name="dataId">数据标识</param>
    /// <param name="password">密码</param>
    /// <param name="operatorCode">操作者代码</param>
    /// <param name="payload">有效载荷数据</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> WriteAsync(string address, uint dataId, uint password, uint operatorCode, ReadOnlySpan<byte> payload, CancellationToken ct = default);

    /// <summary>
    /// 写入设备数据（使用自定义命令码，字符串地址，支持多地址）
    /// </summary>
    /// <param name="command">命令码</param>
    /// <param name="addresses">设备地址字符串（支持格式化多地址）</param>
    /// <param name="dataId">数据标识</param>
    /// <param name="password">密码</param>
    /// <param name="operatorCode">操作者代码</param>
    /// <param name="payload">有效载荷数据（字节数组版本，用于方法兼容）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    IAsyncEnumerable<SemanticValue> WriteAsync(uint command, string addresses, uint dataId, uint password, uint operatorCode, byte[] payload, CancellationToken ct = default);


    /// <summary>
    /// 销毁
    /// </summary>
    void Dispose();


}