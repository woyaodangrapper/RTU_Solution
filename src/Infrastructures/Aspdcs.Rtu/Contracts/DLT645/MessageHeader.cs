using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Aspdcs.Rtu.Contracts.DLT645;

/// <summary>
/// DLT645 数据结构
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MessageHeader : IEquatable<MessageHeader>
{
    private static readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

    /// <summary>
    /// 前导码 FE FE，可变长度 0、2 或 4
    /// </summary>
    public byte[] Preamble;

    public byte StartCode;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
    public byte[] Address;

    public byte FrameStart;
    public byte Code;
    public byte Length;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
    public byte[] Data;

    public byte Checksum;
    public byte EndCode;

    public MessageHeader(byte[] address, byte control, byte[]? bytes = null, byte[]? preamble = null)
    {
        this = default;
        Preamble = preamble ?? [0xFE, 0xFE];
        StartCode = 0x68;
        Address = address ?? new byte[6];
        FrameStart = 0x68;
        Code = control;
        Length = (byte)(bytes?.Length ?? 0);
        Data = bytes ?? [];
        Checksum = 0;
        EndCode = 0x16;
    }

    public MessageHeader(ReadOnlySpan<byte> bytes)
    {
        this = default;
        int preambleLength = bytes.Length >= 2 && bytes[0] == 0xFE ? 2 : 0;
        Preamble = bytes.Slice(0, preambleLength).ToArray();
        StartCode = bytes[preambleLength];
        Address = bytes.Slice(preambleLength + 1, 6).ToArray();
        FrameStart = bytes[preambleLength + 7];
        Code = bytes[preambleLength + 8];
        Length = bytes[preambleLength + 9];
        Data = bytes.Slice(preambleLength + 10, Length).ToArray();
        Checksum = bytes[preambleLength + 10 + Length];
        EndCode = bytes[preambleLength + 11 + Length];
    }

    public readonly int ToBytes(out byte[] buffer)
    {
        int preambleLength = Preamble?.Length ?? 0;
        int length = 12 + Length + preambleLength; // 12 = StartCode+Address+FrameStart+Code+Length+Checksum+EndCode

        buffer = _bytePool.Rent(Math.Max(512, length));

        try
        {
            var span = buffer.AsSpan(0, length);

            // 前导码
            Preamble?.CopyTo(span[..preambleLength]);

            int offset = preambleLength;
            span[offset++] = StartCode;
            Address.CopyTo(span.Slice(offset, 6));
            offset += 6;
            span[offset++] = FrameStart;
            span[offset++] = Code;
            span[offset++] = Length;

            // 数据域
            Data.CopyTo(span.Slice(offset, Length));
            offset += Length;

            // 校验码：第一个起始码 68 到数据域末尾
            var checksumStart = preambleLength; // 第一个 68
            int checksumEnd = offset; // 数据域末尾
            span[offset++] = CalculateChecksum(span[checksumStart..checksumEnd]);

            span[offset++] = EndCode;

            return offset;
        }
        catch
        {
            _bytePool.Return(buffer);
            throw;
        }
    }

    public readonly Memory<byte> ToMemory()
    {
        int length = 12 + Length + (Preamble?.Length ?? 0);
        var buffer = new byte[length];
        WriteToSpan(buffer.AsSpan());
        return buffer;
    }

    public readonly byte[] ToBytes()
    {
        int length = 12 + Length + (Preamble?.Length ?? 0);
        var buffer = new byte[length];
        WriteToSpan(buffer.AsSpan());
        return buffer;
    }

    public readonly int ToSpan(Span<byte> span) => WriteToSpan(span);

    private readonly int WriteToSpan(Span<byte> span)
    {
        int preambleLength = Preamble?.Length ?? 0;
        int requiredLength = 12 + Length + preambleLength; // StartCode + Address + FrameStart + Code + Length + Checksum + EndCode

        if (span.Length < requiredLength)
            throw new ArgumentException($"Span too small, need {requiredLength} bytes.", nameof(span));

        // 前导码
        Preamble?.CopyTo(span[..preambleLength]);

        int offset = preambleLength;
        span[offset++] = StartCode;
        Address.CopyTo(span.Slice(offset, 6));
        offset += 6;
        span[offset++] = FrameStart;
        span[offset++] = Code;
        span[offset++] = Length;

        Data.CopyTo(span.Slice(offset, Length));
        offset += Length;

        // 校验码
        span[offset++] = CalculateChecksum(span[preambleLength..offset]);
        span[offset++] = EndCode;

        return offset; // 实际写入长度
    }

    private static byte CalculateChecksum(Span<byte> data)
    {
        byte checksum = 0;
        foreach (var b in data)
            checksum += b;
        return checksum;
    }

    // IEquatable 实现保持不变
    public override readonly bool Equals(object? obj) => obj is MessageHeader other && Equals(other);

    public readonly bool Equals(MessageHeader other)
    {
        return Address.SequenceEqual(other.Address) &&
               Code == other.Code &&
               Length == other.Length &&
               Data.SequenceEqual(other.Data);
    }

    public override readonly int GetHashCode() => HashCode.Combine(Address, Code, Length, Data);

    public static bool operator ==(MessageHeader left, MessageHeader right) => left.Equals(right);
    public static bool operator !=(MessageHeader left, MessageHeader right) => !(left == right);
}

public static class DataBuilder
{
    /// <summary>
    /// 14H —— 写数据
    /// DATA = 数据标识(4) + 密码(4) + 操作者代码(4) + 数据内容(m)
    /// </summary>
    public static byte[] Write(
        uint dataId,
        uint password,
        uint operatorCode,
        ReadOnlySpan<byte> payload)
    {
        var buffer = new byte[12 + payload.Length];

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0, 4), dataId);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(4, 4), password);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(8, 4), operatorCode);

        payload.CopyTo(buffer.AsSpan(12));

        return buffer;
    }

    /// <summary>
    /// 11H —— 读数据请求
    /// DATA = 数据标识(4) [+ 帧序号(1)]
    /// </summary>
    public static byte[] Read(
        uint dataId,
        byte? frameIndex = null)
    {
        if (frameIndex is null)
        {
            var buffer = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, dataId);
            return buffer;
        }

        var bufferWithIndex = new byte[5];
        BinaryPrimitives.WriteUInt32LittleEndian(bufferWithIndex.AsSpan(0, 4), dataId);
        bufferWithIndex[4] = frameIndex.Value;
        return bufferWithIndex;
    }

    /// <summary>
    /// 12H —— 读后续数据
    /// DATA = 帧序号(1)
    /// </summary>
    public static byte[] ReadNext(byte frameIndex)
        => [frameIndex];

}


// ========================== 数据命令说明 ==========================
//
// 写数据（14H）
// 数据域结构：
//   数据标识        4B
//   密码            4B
//   操作者代码      4B
//   数据内容        mB（长度可变）
//
// 读数据请求（11H）
// 数据域结构：
//   数据标识        4B
//   （可选）块/帧序号 1B
//
// 读后续数据（12H）
// 数据域结构：
//   帧序号          1B
//
// ==================================================================

/* Offset | 字段名       | 大小 | 用途
---------|--------------|------|-------------------------
0        | DataId        | 4B   | 数据标识
4        | Password      | 4B   | 密码 / 鉴权码
8        | OperatorCode  | 4B   | 操作者代码
12       | Payload       | mB   | 实际写入的数据内容
*/

/* Offset | 字段名       | 大小 | 用途
---------|--------------|------|-------------------------
0        | DataId        | 4B   | 数据标识
4        | FrameIndex    | 1B   | （可选）块/帧序号
*/

/* Offset | 字段名       | 大小 | 用途
---------|--------------|------|-------------------------
0        | FrameIndex    | 1B   | 请求的后续数据帧序号
*/
