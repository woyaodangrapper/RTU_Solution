using Microsoft.Extensions.Logging;

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

#if NET6_0_OR_GREATER

using RJCP.IO.Ports;

#else
using System.IO.Ports;
#endif

using ThrowHelper = Aspdcs.Rtu.Extensions.ThrowHelper;

namespace Aspdcs.Rtu.DLT645;

/// <summary>
/// 串口通道基类，用于 DLT645 通讯协议。
/// 构造时自动打开所有串口，释放时自动关闭。
/// </summary>
public class Channel : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly ILogger<Channel> _logger;

    protected CircularBuffer Buffer { get; }

    private bool _disposed;

    public ChannelOptions Options { get; }

    /// <summary>
    /// 通道名称（唯一标识）
    /// </summary>
    public string ChannelName => Options.ChannelName;

#if NET6_0_OR_GREATER

    /// <summary>
    /// 获取可用串行端口的集合。
    /// </summary>
    /// <remarks>该集合是只读的，反映了当前检测到的串行端口集
    /// 如果在应用程序运行时添加或删除端口，则内容可能会更改。</remarks>
    public Collection<SerialPortStream> Ports { get; } = [];

#else
    /// <summary>
    /// 获取可用串行端口的集合。
    /// </summary>
    /// <remarks>该集合是只读的，反映了当前检测到的串行端口集
    /// 如果在应用程序运行时添加或删除端口，则内容可能会更改。</remarks>
    public Collection<SerialPort> Ports { get; } = [];
#endif

    /// <summary>
    /// 当前通道下所有串口（只读）
    /// </summary>
    public ReadOnlyCollection<ComChannel> Channels => Options.Channels;

    private static readonly Action<ILogger, string, Exception?> LogPortOpen =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(101, nameof(LogPortOpen)),
            "Serial port {PortName} opened.");

    private static readonly Action<ILogger, string, Exception?> LogPortClose =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(102, nameof(LogPortClose)),
            "Serial port {PortName} closed.");

    private static readonly Action<ILogger, string, Exception?> LogPortError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(103, nameof(LogPortError)),
            "Serial port {PortName} encountered an error.");

    private static readonly Action<ILogger, string, int, Exception?> LogChannelInitialized =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(104, nameof(LogChannelInitialized)),
            "Channel '{ChannelName}' initialized with {Count} serial ports.");

    protected Channel([NotNull] ChannelOptions options, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Channel>();

        Options = options ?? throw new ArgumentNullException(nameof(options));
        Buffer = new CircularBuffer(options.MaxLength);
    }

    // 改为同步方法
    protected virtual void Create()
    {
        foreach (var com in Options.Channels.Distinct())
        {
            // 同步探测串口
            var port = SerialPortExtensions.AutoNegotiate(
                portName: com.Port,
                timeout: Options.Timeout,
                Options.Port
            );

            Ports.Add(port);

            try
            {
                port.Open();
                LogPortOpen(_logger, port.PortName, null);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException
                                          or ArgumentException
                                          or InvalidOperationException
                                          or IOException)
            {
                LogPortError(_logger, port.PortName, ex);
                Dispose();
                throw;
            }
        }
        LogChannelInitialized(_logger, ChannelName, Ports.Count, null);
    }

#if NET6_0_OR_GREATER

    /// <summary>
    /// 检查串口是否已连接并可用。
    /// </summary>
    /// <param name="port">要检查的串口</param>
    /// <returns>如果串口已打开且可用则返回 true；否则返回 false</returns>
    protected virtual bool IsConnected([NotNull] SerialPortStream port)
#else
    /// <summary>
    /// 检查串口是否已连接并可用。
    /// </summary>
    /// <param name="port">要检查的串口</param>
    /// <returns>如果串口已打开且可用则返回 true；否则返回 false</returns>
    protected virtual bool IsConnected([NotNull] SerialPort port)
#endif

    {
        ThrowHelper.ThrowIfNull(port);

        // 检查串口是否打开
        if (!port.IsOpen)
            return false;

        try
        {
            _ = port.CtsHolding; // 查询 CTS 信号线状态
            _ = port.DsrHolding; // 查询 DSR 信号线状态
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }

        return true;
    }

    public bool IsPortsConnected()
    {
        ThrowHelper.ThrowIf(_disposed, this);
        return Ports.All(IsConnected);
    }

    /// <summary>
    /// 写入数据到指定串口。
    /// </summary>
    public int Write(string comPort, Span<byte> buffer)
    {
        ThrowHelper.ThrowIf(_disposed, this);

        var port = Ports.FirstOrDefault(p => p.PortName.Equals(comPort, StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException($"Port {comPort} not found.");

        if (!port.IsOpen)
            throw new InvalidOperationException($"Port {comPort} is not open.");

        // 清除旧的或残留的输入数据
        port.DiscardInBuffer();

        // 清除输出缓冲区（可选，但推荐）
        port.DiscardOutBuffer();

        buffer.TryGetData(out Span<byte> data);
        MessageHeaderExtensions.EncodeData(data);

        port.Write(buffer);
        return buffer.Length;
    }

    public int Write(string comPort, byte[] buffer, int offset, int count)
        => Write(comPort, buffer.AsSpan(offset, count));

    public int Write(string comPort, byte[] buffer)
        => Write(comPort, buffer.AsSpan());

    /// <summary>
    /// 从指定串口读取数据。
    /// </summary>
    public int Read(string comPort, Span<byte> buffer)
    {
        ThrowHelper.ThrowIf(_disposed, this);

        var port = Ports.FirstOrDefault(p => p.PortName.Equals(comPort, StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException($"Port {comPort} not found.");

        if (!port.IsOpen)
            throw new InvalidOperationException($"Port {comPort} is not open.");
        return port.Read(buffer);
    }

    public int Read(string comPort, byte[] buffer, int offset, int count)
        => Read(comPort, buffer.AsSpan(offset, count));

    public int Read(string comPort, byte[] buffer)
        => Read(comPort, buffer.AsSpan());

    /// <summary>
    /// 异步写入数据到指定串口。
    /// </summary>
    public async Task<int> WriteAsync(
        string comPort,
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIf(_disposed, this);

        var port = Ports.FirstOrDefault(p => p.PortName.Equals(comPort, StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException($"Port {comPort} not found.");

        if (!port.IsOpen)
            throw new InvalidOperationException($"Port {comPort} is not open.");

        // 清除旧输入数据
        port.DiscardInBuffer();

        // 清除输出缓冲区
        port.DiscardOutBuffer();

        // 编码数据
        if (buffer.TryGetData(out Memory<byte> data))
            MessageHeaderExtensions.EncodeData(data.Span);

        await port.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        await port.FlushAsync(cancellationToken).ConfigureAwait(false);

        return buffer.Length;
    }

    public Task<int> WriteAsync(
        string comPort,
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken = default)
        => WriteAsync(comPort, buffer.AsMemory(offset, count), cancellationToken);

    public Task<int> WriteAsync(
        string comPort,
        byte[] buffer,
        CancellationToken cancellationToken = default)
        => WriteAsync(comPort, buffer.AsMemory(), cancellationToken);

    /// <summary>
    /// 异步从指定串口读取数据。
    /// </summary>
    public async Task<int> ReadAsync(
        string comPort,
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIf(_disposed, this);

        var port = Ports.FirstOrDefault(p => p.PortName.Equals(comPort, StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException($"Port {comPort} not found.");

        if (!port.IsOpen)
            throw new InvalidOperationException($"Port {comPort} is not open.");

        return await port.ReadAsync(buffer, cancellationToken)
                         .ConfigureAwait(false);
    }

    public Task<int> ReadAsync(
        string comPort,
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken = default)
        => ReadAsync(comPort, buffer.AsMemory(offset, count), cancellationToken);

    public Task<int> ReadAsync(
        string comPort,
        byte[] buffer,
        CancellationToken cancellationToken = default)
        => ReadAsync(comPort, buffer.AsMemory(), cancellationToken);

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 关闭并释放所有串口
                foreach (var port in Ports)
                {
                    try
                    {
                        if (port.IsOpen)
                        {
                            port.Close();
                            LogPortClose(_logger, port.PortName, null);
                        }
                        port.Dispose();
                    }
                    catch (IOException ex)
                    {
                        LogPortError(_logger, port.PortName, ex);
                    }
                }

                _cancellation.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Channel()
    {
        Dispose(false);
    }
}