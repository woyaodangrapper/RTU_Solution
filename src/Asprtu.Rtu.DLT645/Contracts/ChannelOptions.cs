using Asprtu.Rtu.DLT645.Extensions;
using System.Collections.ObjectModel;
using System.IO.Ports;

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
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// 获取或设置重试次数。
    /// 默认值 1 次。
    /// </summary>
    public int RetryCount { get; init; } = 1;


    /// <summary>
    /// 获取或设置帧的最大长度（用于缓冲区分配）。
    /// 默认值 256 字节，大于协议标准限制的 216 字节 (L=200)。
    /// </summary>
    public int MaxLength { get; init; } = 256;

    /// <summary>
    /// 获取或设置用于设备连接的通信端口设置。
    /// </summary>
    public ComOptions Port { get; set; } = new();

}
public class ComOptions
{

    /// <summary>
    /// 是否启用自动协商参数
    /// </summary>
    public bool Auto { get; set; } = true;

    /// <summary>
    /// 波特率，AutoNegotiate = true 时可忽略
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// 奇偶校验，AutoNegotiate = true 时可忽略
    /// </summary>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>
    /// 数据位，AutoNegotiate = true 时可忽略
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// 停止位，AutoNegotiate = true 时可忽略
    /// </summary>
    public StopBits StopBits { get; set; } = StopBits.One;

}

/// <summary>
/// DLT645 通道配置构建器
/// </summary>
public sealed class CreateBuilder
{
    private readonly string _channelName;
    private readonly List<ComChannel> _channels = [];
    private TimeSpan _frameTimeout = TimeSpan.FromMilliseconds(500); // 单帧超时，默认 500ms
    private int _retryCount = 1;     // 默认重试 1 次
    private int _maxLength = 1024;    // 帧的最大长度
    private ComOptions _port = new(); // 默认 COM 端口设置

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
    /// 配置构建器以使用指定的 COM 端口设置。
    /// </summary>
    /// <param name="options">要应用于构建器的 COM 端口设置。不能为空。</param>
    /// <returns>更新了 COM 配置的构建器实例</returns>
    public CreateBuilder WithCom(ComOptions options)
    {
        _port = options;
        return this;
    }

    /// <summary>
    /// 添加串口通道并绑定地址列表。
    /// </summary>
    /// <param name="comPort">串口名称</param>
    /// <param name="addresses">设备地址列表</param>
    /// <returns>当前构建器实例</returns>
    public CreateBuilder WithChannel(string comPort, params byte[] addresses)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comPort, nameof(comPort));
        _channels.Add(new ComChannel(comPort, addresses));
        return this;
    }

    /// <summary>
    /// 设置是否启用自动协商参数
    /// </summary>
    public CreateBuilder WithAuto(bool auto)
    {
        _port.Auto = auto;
        return this;
    }

    /// <summary>
    /// 设置波特率
    /// </summary>
    public CreateBuilder WithBaudRate(int baudrate)
    {
        _port.BaudRate = baudrate;
        return this;
    }

    /// <summary>
    /// 设置奇偶校验
    /// </summary>
    public CreateBuilder WithParity(Parity parity)
    {
        _port.Parity = parity;
        return this;
    }

    /// <summary>
    /// 设置数据位
    /// </summary>
    public CreateBuilder WithDataBits(int dataBits)
    {
        _port.DataBits = dataBits;
        return this;
    }

    /// <summary>
    /// 设置停止位
    /// </summary>
    public CreateBuilder WithStopBits(StopBits stopBits)
    {
        _port.StopBits = stopBits;
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
    /// 添加串口通道（支持字符串格式的地址）。
    /// </summary>
    /// <param name="comPort">串口名称（如 COM1）</param>
    /// <returns>当前构建器实例</returns>
    public CreateBuilder WithChannel(string comPort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comPort, nameof(comPort));
        _channels.Add(new ComChannel(comPort, null));
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
        _frameTimeout = new TimeSpan(0, 0, 0, 0, milliseconds);
        return this;
    }
    /// <summary>
    /// 设置单帧超时时间。
    /// </summary>
    /// <param name="timeSpan">时间间隔（1 tick = 100 纳秒）</param>
    /// <returns>当前构建器实例</returns>
    public CreateBuilder WithTimeout(TimeSpan timeSpan)
    {
        _frameTimeout = timeSpan;
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
    /// 使用指定的帧的最大长度 。
    /// </summary>
    /// <param name="maxFrameLength">最大长度,协议标准 216 字节 (L=200)/param>
    /// <returns>当前构建器实例。</returns>
    public CreateBuilder WithMaxLength(int maxFrameLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFrameLength, 1, nameof(maxFrameLength));
        _maxLength = maxFrameLength;
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
        MaxLength = _maxLength,
        Port = _port
    };
}
