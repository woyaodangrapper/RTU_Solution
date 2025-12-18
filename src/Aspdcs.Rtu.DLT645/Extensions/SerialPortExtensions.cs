using Aspdcs.Rtu.Contracts.DLT645;
using System.Buffers;

#if NET6_0_OR_GREATER

using RJCP.IO.Ports;

#else
using System.IO.Ports;
#endif

namespace Aspdcs.Rtu.DLT645.Extensions;

internal static class SerialPortExtensions
{
    /// <summary>
    /// 配置一组DLT645串口常用的奇偶校验、波特率、数据位、
    /// 和停止位设置。
    /// </summary>
    /// <remarks>这些候选者可用于尝试与可能需要的串行设备进行通信
    /// 不同的标准配置。该阵列包括常见组合，例如具有偶校验的 8 个数据位
    /// 在 2400 或 9600 波特率下，以及在相同波特率下具有偶校验的 7 个数据位。</remarks>
    private static readonly (Parity parity, int baud, int databits, StopBits stopBits)[] _candidates =
    [
        // 8E1 @ 2400
        (Parity.Even, 2400, 8, StopBits.One),

        // 8E1 @ 9600
        (Parity.Even, 9600, 8, StopBits.One),

        // 7E1 @ 2400
        (Parity.Even, 2400, 7, StopBits.One),

        // 7E1 @ 9600
        (Parity.Even, 9600, 7, StopBits.One),
    ];

#if NET6_0_OR_GREATER

    /// <summary>
    /// 自动探测串口是否能正常与 DLT645 从机通讯
    /// </summary>
    /// <param name="portName"></param>
    /// <param name="timeout"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static SerialPortStream AutoNegotiate(
        string portName,
        TimeSpan timeout,
        ComOptions options)
    {
        if (!options.Auto)
        {
            return new(portName, options.BaudRate, options.DataBits, options.Parity, options.StopBits)
            {
                ReadTimeout = (int)timeout.TotalMilliseconds,
                WriteTimeout = (int)timeout.TotalMilliseconds
            };
        }

        MessageHeader messageHeader = new(
           address: [.. Enumerable.Repeat((byte)0xAA, 6)],
           control: ((byte)Command.Code.ReadAddress),
           bytes: []
        );

        byte[] messageBytes = messageHeader.ToBytes();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(32);

        try
        {
            foreach (var (parity, baud, databits, stopBits) in _candidates)
            {
                using SerialPortStream port = new(portName, baud, databits, parity, stopBits)
                {
                    ReadTimeout = (int)timeout.TotalMilliseconds,
                    WriteTimeout = (int)timeout.TotalMilliseconds
                };

                try
                {
                    port.Open();
                    port.DiscardInBuffer();
                    port.DiscardOutBuffer();
                    port.Write(messageBytes);

                    int read = port.Read(buffer, 0, buffer.Length);

                    if (IsValid(buffer.AsSpan(0, read)))
                    {
                        var result = ClonePort(port);
                        ArrayPool<byte>.Shared.Return(buffer);
                        return result;
                    }
                }
                catch (Exception ex) when (ex is IOException
                                           or UnauthorizedAccessException
                                           or TimeoutException)
                {
                    // 忽略,继续尝试
                }
                finally
                {
                    port.Close();
                }
            }

            throw new InvalidOperationException($"无法与设备建立 DLT645 通讯(端口:{portName})。");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static SerialPortStream ClonePort(SerialPortStream p)
        => new(p.PortName, p.BaudRate, p.DataBits, p.Parity, p.StopBits)
        {
            ReadTimeout = p.ReadTimeout,
            WriteTimeout = p.WriteTimeout
        };

#else
    public static SerialPort AutoNegotiate(string portName, TimeSpan timeout, ComOptions options)
    {
        if (!options.Auto)
        {
            return new SerialPort(portName, options.BaudRate, options.Parity, options.DataBits, options.StopBits)
            {
                ReadTimeout = (int)timeout.TotalMilliseconds,
                WriteTimeout = (int)timeout.TotalMilliseconds
            };
        }

        MessageHeader messageHeader = new(
            address: [.. Enumerable.Repeat((byte)0xAA, 6)],
            control: (byte)Command.Code.ReadAddress,
            bytes: []
        );

        byte[] messageBytes = messageHeader.ToBytes();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(32);

        try
        {
            foreach (var (parity, baud, databits, stopBits) in _candidates)
            {
                using var port = new SerialPort(portName, baud, parity, databits, stopBits)
                {
                    ReadTimeout = (int)timeout.TotalMilliseconds,
                    WriteTimeout = (int)timeout.TotalMilliseconds
                };

                try
                {
                    port.Open();
                    port.DiscardInBuffer();
                    port.DiscardOutBuffer();
                    port.Write(messageBytes, 0, messageBytes.Length);

                    int read = port.Read(buffer, 0, buffer.Length);

                    if (IsValid(buffer.AsSpan(0, read)))
                    {
                        var result = ClonePort(port);
                        ArrayPool<byte>.Shared.Return(buffer);
                        return result;
                    }
                }
                catch (Exception ex) when (ex is IOException
                                           or UnauthorizedAccessException
                                           or TimeoutException)
                {
                    // 忽略并尝试下一个组合
                }
                finally
                {
                    if (port.IsOpen)
                        port.Close();
                }
            }

            throw new InvalidOperationException($"无法与设备建立 DLT645 通讯(端口:{portName})。");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    private static SerialPort ClonePort(SerialPort p)
    => new(p.PortName, p.BaudRate, p.Parity, p.DataBits, p.StopBits)
    {
        ReadTimeout = p.ReadTimeout,
        WriteTimeout = p.WriteTimeout
    };

    public static int Read(this SerialPort port, Span<byte> buffer)
    {
        if (port == null) throw new ArgumentNullException(nameof(port));
        if (!port.IsOpen) throw new InvalidOperationException("Port is not open");

        byte[] temp = new byte[buffer.Length];
        int read = port.Read(temp, 0, temp.Length);
        temp.AsSpan(0, read).CopyTo(buffer);
        return read;
    }

    public static void Write(this SerialPort port, ReadOnlySpan<byte> buffer)
    {
        if (port == null) throw new ArgumentNullException(nameof(port));
        if (!port.IsOpen) throw new InvalidOperationException("Port is not open");

        byte[] temp = buffer.ToArray();
        port.Write(temp, 0, temp.Length);
    }

    public static async Task<int> ReadAsync(this SerialPort port, Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (port == null) throw new ArgumentNullException(nameof(port));
        if (!port.IsOpen) throw new InvalidOperationException("Port is not open");

        return await port.BaseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    public static async Task WriteAsync(this SerialPort port, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (port == null) throw new ArgumentNullException(nameof(port));
        if (!port.IsOpen) throw new InvalidOperationException("Port is not open");

        await port.BaseStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 异步刷新串口缓冲区（空实现，仅兼容 API）
    /// </summary>
    public static Task FlushAsync(this SerialPort port, CancellationToken cancellationToken = default) => Task.CompletedTask;
#endif

    /// <summary>
    /// 判断 68 起始 + CS 校验是否正确
    /// </summary>
    private static bool IsValid(ReadOnlySpan<byte> span)
    {
        if (span.Length < 10) return false;   // 最小帧长度
        int idx = span.IndexOf((byte)0x68);// 跳过可变前导 FE
        if (idx < 0 || span.Length - idx < 10) return false;

        // 检查第二个 0x68 是否存在
        if (span[idx + 7] != 0x68) return false;

        return true;
    }
}