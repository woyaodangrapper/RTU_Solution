using Asprtu.Rtu.Contracts.DLT645;
using Asprtu.Rtu.DLT645.Contracts;
using System.IO.Ports;

namespace Asprtu.Rtu.DLT645.Extensions;

internal class SerialPortExtensions
{
    /// <summary>
    /// 配置一组DLT645串口常用的奇偶校验、波特率、数据位、
    /// 和停止位设置。
    /// </summary>
    /// <remarks>These candidates can be used to attempt communication with serial devices that may require
    /// different standard configurations. The array includes common combinations such as 8 data bits with even parity
    /// at 2400 or 9600 baud, and 7 data bits with even parity at the same baud rates.</remarks>
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
    /// <summary>
    /// 自动探测串口是否能正常与 DLT645 从机通讯
    /// </summary>
    public static async Task<SerialPort> AutoNegotiateAsync(
        string portName,
        TimeSpan timeout,
        ComOptions options,
        CancellationToken cancellation = default)
    {
        if (!options.Auto)
        {

            return new SerialPort(portName, options.BaudRate, options.Parity, options.DataBits, options.StopBits)
            {
                ReadTimeout = (int)timeout.TotalMilliseconds,
                WriteTimeout = (int)timeout.TotalMilliseconds
            };
        }
        foreach (var (parity, baud, databits, stopBits) in _candidates)
        {
            using SerialPort port = new(portName, baud, parity, databits, stopBits)
            {
                ReadTimeout = (int)timeout.TotalMilliseconds,
                WriteTimeout = (int)timeout.TotalMilliseconds
            };
            // 广播帧
            MessageHeader messageHeader = new(
               address: [.. Enumerable.Repeat((byte)0xAA, 6)],
               control: ((byte)Command.ControlCode.ReadAddress),
               bytes: []
            );

            int length = messageHeader.ToBytes(out var bytes);
            try
            {
                port.Open();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                cts.CancelAfter(timeout); // 超时自动取消
                await port.BaseStream.WriteAsync(bytes, cts.Token).ConfigureAwait(true);
                await port.BaseStream.FlushAsync(cts.Token).ConfigureAwait(true);


                byte[] buffer = new byte[32];
                int read = await port.BaseStream.ReadAsync(buffer, cancellation)
                    .ConfigureAwait(false);

                if (IsValid(buffer.AsSpan(0, read)))
                    return ClonePort(port);
            }
            catch (IOException)
            {
                // 忽略错误继续试
            }
            catch (UnauthorizedAccessException)
            {
                // 忽略错误继续试
            }
            catch (OperationCanceledException)
            {
                // 可以安全捕获超时或取消
            }
            catch (TimeoutException)
            {
                // 忽略错误继续试
            }
        }

        throw new InvalidOperationException($"无法与设备建立 DLT645 通讯（端口：{portName}）。");
    }

    private static SerialPort ClonePort(SerialPort p)
    {
        // 重新打开一个真正用于后续通信的 SerialPort
        var port = new SerialPort(p.PortName, p.BaudRate, p.Parity, p.DataBits, p.StopBits)
        {
            ReadTimeout = p.ReadTimeout,
            WriteTimeout = p.WriteTimeout
        };
        return port;
    }

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
