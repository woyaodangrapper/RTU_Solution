using Asprtu.Rtu.DLT645.Contracts;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;

namespace Asprtu.Rtu.DLT645;

/// <summary>
/// 串口通道基类，用于 DLT645 通讯协议。
/// 构造时自动打开所有串口，释放时自动关闭。
/// </summary>
public class Channel : IDisposable
{
    private readonly List<SerialPort> _ports = [];
    private readonly CancellationTokenSource _cancellation = new();
    private readonly ILogger<Channel> _logger;
    private bool _disposed;

    public ChannelOptions Options { get; }

    /// <summary>
    /// 通道名称（唯一标识）
    /// </summary>
    public string ChannelName => Options.ChannelName;

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

    public Channel([NotNull] ChannelOptions options, ILoggerFactory loggerFactory)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = loggerFactory.CreateLogger<Channel>();

        // 初始化并打开所有串口
        foreach (var com in options.Channels.Distinct())
        {
            var port = new SerialPort(
                com.Port,
                options.BaudRate,
                Parity.None,
                8,
                StopBits.One)
            {
                ReadTimeout = options.Timeout,
                WriteTimeout = options.Timeout
            };

            _ports.Add(port);

            // 立即打开串口
            try
            {
                port.Open();
                LogPortOpen(_logger, port.PortName, null);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogPortError(_logger, port.PortName, ex);
                Dispose();
                throw;
            }
            catch (ArgumentException ex)
            {
                LogPortError(_logger, port.PortName, ex);
                Dispose();
                throw;
            }
            catch (InvalidOperationException ex)
            {
                LogPortError(_logger, port.PortName, ex);
                Dispose();
                throw;
            }
            catch (IOException ex)
            {
                LogPortError(_logger, port.PortName, ex);
                Dispose();
                throw;
            }
        }

        LogChannelInitialized(_logger, ChannelName, _ports.Count, null);
    }

    /// <summary>
    /// 检查串口是否已连接并可用。
    /// </summary>
    /// <param name="port">要检查的串口</param>
    /// <returns>如果串口已打开且可用则返回 true；否则返回 false</returns>
    protected virtual bool IsConnected([NotNull] SerialPort port)
    {
        ArgumentNullException.ThrowIfNull(port);

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

    /// <summary>
    /// 写入数据到指定串口。
    /// </summary>
    public int Write(string comPort, byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var port = _ports.Find(p => p.PortName.Equals(comPort, StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException($"Port {comPort} not found.");

        if (!port.IsOpen)
            throw new InvalidOperationException($"Port {comPort} is not open.");

        port.Write(buffer, offset, count);
        return count;
    }

    /// <summary>
    /// 从指定串口读取数据。
    /// </summary>
    public int Read(string comPort, byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var port = _ports.Find(p => p.PortName.Equals(comPort, StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException($"Port {comPort} not found.");

        if (!port.IsOpen)
            throw new InvalidOperationException($"Port {comPort} is not open.");

        return port.Read(buffer, offset, count);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 关闭并释放所有串口
                foreach (var port in _ports)
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