using Aspdcs.Rtu.Contracts.DLT645;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static Aspdcs.Rtu.DLT645.Serialization.Command;
using ThrowHelper = Aspdcs.Rtu.Extensions.ThrowHelper;

namespace Aspdcs.Rtu.DLT645.Extensions;

public static class Dlt645Client1997Extensions
{
    /// <summary>
    /// 读取1997设备数据总电能
    /// </summary>
    /// <param name="channel">Dlt645Client</param>
    /// <param name="address">设备地址</param>
    /// <param name="command">控制码</param>
    /// <param name="dataId">业务码</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>DLT645-1997 从机返回帧</returns>
    public static async IAsyncEnumerable<MessageHeader> Read1997Async([NotNull] this IDlt645Client channel, string address, uint command, uint dataId,
         [EnumeratorCancellation] CancellationToken ct = default)
    {
        var commandByte = Convert.ToByte(command);

        ushort lowDi = (ushort)(dataId & 0xFFFF); // 取低 2 字节

        foreach (var addr in AddressFormatExtension.FormatAddresses(address))
        {

            // addr 是字节数组 6 位
            MessageHeader messageHeader = new(
                address: addr,
                control: commandByte,
                bytes: [
                    (byte)(lowDi & 0xFF),        // 低字节
                    (byte)((lowDi >> 8) & 0xFF)  // 高字节
                ]
            );
            var length = messageHeader.ToBytes(out var messageBytes);

            await foreach (var messages in channel.TryWriteAsync(messageBytes.AsMemory(0, length), ct).ConfigureAwait(false))
            {
                if (!IsValid(messages.ToBytes()))
                {
                    throw new InvalidOperationException("返回数据格式错误");
                }

                yield return messages;
            }
        }

    }



    /// <summary>
    /// 读取1997设备数据总电能
    /// </summary>
    /// <param name="channel">Dlt645Client</param>
    /// <param name="address">设备地址</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    public static async IAsyncEnumerable<SemanticValue> Read1997Async([NotNull] this IDlt645Client channel, string address,
         [EnumeratorCancellation] CancellationToken ct = default)
    {
        var commandByte = Convert.ToByte(Code.ReadData);

        ushort lowDi = (ushort)((uint)Legacy1997.TotalActiveEnergy & 0xFFFF); // 取低 2 字节
        var bytes = new byte[]
        {
            (byte)(lowDi & 0xFF),        // 低字节
            (byte)((lowDi >> 8) & 0xFF)  // 高字节
        };

        foreach (var addr in AddressFormatExtension.FormatAddresses(address))
        {

            // addr 是字节数组 6 位
            MessageHeader messageHeader = new(
                address: addr,
                control: commandByte,
                bytes
            );
            var length = messageHeader.ToBytes(out var messageBytes);

            await foreach (var messages in channel.TryWriteAsync(messageBytes.AsMemory(0, length), ct).ConfigureAwait(false))
            {
                if (!IsValid(messages.ToBytes()))
                {
                    throw new InvalidOperationException("返回数据格式错误");
                }

                int sum = 0;
                foreach (var b in messages.Data)
                {
                    // 拆高低位
                    int high = (b >> 4) & 0x0F;
                    int low = b & 0x0F;
                    sum = sum * 100 + high * 10 + low;
                }

                DataFormats.TryGet((uint)Legacy1997.TotalActiveEnergy, out var def);
                ThrowHelper.ThrowIfNull(def);

                yield return new NumericValue("", sum, def!.Unit, def!.Format);
            }
        }

    }

    /// <summary>
    /// 读取1997设备数据总电能
    /// </summary>
    /// <param name="channel">Dlt645Client</param>
    /// <param name="addresses">设备地址</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>返回语义值序列</returns>
    public static async IAsyncEnumerable<SemanticValue> Read1997Async([NotNull] this IDlt645Client channel, [NotNull] IEnumerable<AddressValue> addresses,
         [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var addr in addresses)
        {
            await foreach (var item in channel.Read1997Async(addr.Address, ct).WithCancellation(ct))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// 读取1997设备数据总电能
    /// </summary>
    /// <param name="channel">Dlt645Client</param>
    /// <param name="addresses">设备地址</param>
    /// <param name="command">控制码</param>
    /// <param name="dataId">业务码</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>DLT645-1997 从机返回帧</returns>
    public static async IAsyncEnumerable<MessageHeader> Read1997Async([NotNull] this IDlt645Client channel, [NotNull] IEnumerable<AddressValue> addresses, uint command, uint dataId,
         [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var addr in addresses)
        {
            await foreach (var item in channel.Read1997Async(addr.Address, command, dataId, ct).WithCancellation(ct))
            {
                yield return item;
            }
        }
    }

    private static bool IsValid(ReadOnlySpan<byte> span)
    {
        return span.Length >= 10   // 6字节地址 + 2控制码 + 1长度 + 1结束符
          && span[0] == 0x68
          && span[^1] == 0x16;
    }
}