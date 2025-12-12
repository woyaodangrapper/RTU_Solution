using Asprtu.Rtu.Contracts.DLT645;
using Asprtu.Rtu.DLT645.Contracts;
using RJCP.IO.Ports;
using System.Buffers;

namespace Asprtu.Rtu.DLT645.Extensions;

internal class SerialPortExtensions
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
    /// <summary>
    /// 自动探测串口是否能正常与 DLT645 从机通讯
    /// </summary>
    public static async Task<SerialPortStream> AutoNegotiateAsync(
        string portName,
        TimeSpan timeout,
        ComOptions options,
        CancellationToken cancellation = default)
    {
        if (!options.Auto)
        {
            return new(portName, options.BaudRate, options.DataBits, options.Parity, options.StopBits)
            {
                ReadTimeout = (int)timeout.TotalMilliseconds,
                WriteTimeout = (int)timeout.TotalMilliseconds
            };
        }

        // 广播帧
        MessageHeader messageHeader = new(
           address: [.. Enumerable.Repeat((byte)0xAA, 6)],
           control: ((byte)Command.Code.ReadAddress),
           bytes: []
        );

        messageHeader.ToBytes(out var bytes);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(32); // 足够大
        try
        {
            foreach (var (parity, baud, databits, stopBits) in _candidates)
            {
                using SerialPortStream port = new(portName, baud, databits, parity, stopBits)
                {
                    ReadTimeout = (int)timeout.TotalMilliseconds,
                    WriteTimeout = (int)timeout.TotalMilliseconds
                };
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                cts.CancelAfter(timeout);

                try
                {
                    port.Open();
                    await port.FlushAsync(cts.Token).ConfigureAwait(false);
                    await port.WriteAsync(bytes, cts.Token).ConfigureAwait(false);
                    int read = await port.ReadAsync(buffer, cts.Token).ConfigureAwait(false);

                    if (IsValid(buffer.AsSpan(0, read)))
                    {
                        // 成功匹配，归还 buffer 后返回
                        var result = ClonePort(port);
                        ArrayPool<byte>.Shared.Return(buffer);
                        return result;
                    }
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
                finally
                {
                    port.DiscardInBuffer();
                    port.DiscardOutBuffer();
                }
            }

            throw new InvalidOperationException($"无法与设备建立 DLT645 通讯（端口：{portName}）。");
        }
        finally
        {
            // 确保 buffer 总是被归还
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static SerialPortStream ClonePort(SerialPortStream p)
        => new(p.PortName, p.BaudRate, p.DataBits, p.Parity, p.StopBits)
        {
            ReadTimeout = p.ReadTimeout,
            WriteTimeout = p.WriteTimeout
        };

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
