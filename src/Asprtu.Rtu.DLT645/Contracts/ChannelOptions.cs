using Asprtu.Rtu.DLT645.Extensions;
using System.Collections.ObjectModel;

namespace Asprtu.Rtu.DLT645.Contracts;

/// <summary>
/// DLT645 通道配置选项
/// </summary>
public sealed class ChannelOptions
{
    /// <summary>
    /// 初始化 <see cref="ChannelOptions"/> 类的新实例。
    /// </summary>
    /// <param name="channelName">通道唯一名称</param>
    public ChannelOptions(string channelName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelName, nameof(channelName));
        ChannelName = channelName;
    }

    /// <summary>
    /// 获取通道唯一名称。
    /// </summary>
    public string ChannelName { get; }

    /// <summary>
    /// 获取串口通道列表（只读）。
    /// </summary>
    public ReadOnlyCollection<ComChannel> Channels { get; init; } = new ReadOnlyCollection<ComChannel>([]);

    /// <summary>
    /// 获取或设置单帧超时时间（毫秒）。
    /// 默认值 500 毫秒。
    /// </summary>
    public int Timeout { get; init; } = 500;

    /// <summary>
    /// 获取或设置重试次数。
    /// 默认值 1 次。
    /// </summary>
    public int RetryCount { get; init; } = 1;

    /// <summary>
    /// 获取或设置波特率。
    /// 默认值 2400 bps（DLT645-2007 标准波特率）。
    /// </summary>
    public int BaudRate { get; init; } = 2400;
}

/// <summary>
/// DLT645 通道配置构建器
/// </summary>
public sealed class CreateBuilder
{
    private readonly string _channelName;
    private readonly List<ComChannel> _channels = [];
    private int _frameTimeout = 500; // 单帧超时，默认 500ms
    private int _retryCount = 1;     // 默认重试 1 次
    private int _baudRate = 2400;    // 默认波特率

    /// <summary>
    /// 初始化 <see cref="CreateBuilder"/> 类的新实例。
    /// </summary>
    /// <param name="channelName">通道唯一名称</param>
    public CreateBuilder(string channelName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelName, nameof(channelName));
        _channelName = channelName;
    }

    /// <summary>
    /// 添加串口通道。
    /// </summary>
    /// <param name="comPort">串口名称（如 COM1）</param>
    /// <param name="addresses">该串口下的设备地址列表</param>
    /// <returns>当前构建器实例</returns>
    public CreateBuilder WithChannel(string comPort, params byte[] addresses)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comPort, nameof(comPort));
        _channels.Add(new ComChannel(comPort, addresses));
        return this;
    }

    /// <summary>
    /// 添加串口通道（支持字符串格式的地址）。
    /// </summary>
    /// <param name="comPort">串口名称（如 COM1）</param>
    /// <param name="addresses">设备地址字符串，格式如 "81-00-03-68-90-96" 或 "810003689096"，多个地址用分号分隔</param>
    /// <returns>当前构建器实例</returns>
    public CreateBuilder WithChannel(string comPort, string addresses)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comPort, nameof(comPort));
        ArgumentException.ThrowIfNullOrWhiteSpace(addresses, nameof(addresses));

        var addressList = AddressFormatExtension.FormatAddresses(addresses);
        foreach (var addr in addressList)
            _channels.Add(new ComChannel(comPort, addr));

        return this;
    }


    /// <summary>
    /// 设置单帧超时时间。
    /// </summary>
    /// <param name="milliseconds">超时时间（毫秒）</param>
    /// <returns>当前构建器实例</returns>
    public CreateBuilder WithTimeout(int milliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(milliseconds, 100, nameof(milliseconds));
        _frameTimeout = milliseconds;
        return this;
    }

    /// <summary>
    /// 设置重试次数。
    /// </summary>
    /// <param name="count">重试次数</param>
    /// <returns>当前构建器实例</returns>
    public CreateBuilder WithRetryCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count, nameof(count));
        _retryCount = count;
        return this;
    }

    /// <summary>
    /// 设置波特率。
    /// </summary>
    /// <param name="baudRate">波特率（常用值：1200, 2400, 4800, 9600）</param>
    /// <returns>当前构建器实例</returns>
    public CreateBuilder WithBaudRate(int baudRate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(baudRate, 1200, nameof(baudRate));
        _baudRate = baudRate;
        return this;
    }

    /// <summary>
    /// 构建 <see cref="ChannelOptions"/> 实例。
    /// </summary>
    /// <returns>配置实例</returns>
    public ChannelOptions Build() => new(_channelName)
    {
        Channels = new ReadOnlyCollection<ComChannel>(_channels),
        Timeout = _frameTimeout,
        RetryCount = _retryCount,
        BaudRate = _baudRate
    };
}
