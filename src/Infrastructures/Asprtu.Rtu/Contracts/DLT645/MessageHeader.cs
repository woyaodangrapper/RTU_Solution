using System.Buffers;
using System.Runtime.InteropServices;

namespace Asprtu.Rtu.Contracts.DLT645;

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

    public MessageHeader(byte[] address, byte control, byte[]? bytes, byte[]? preamble = null)
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
            if (Preamble != null)
                Preamble.CopyTo(span[..preambleLength]);

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

    public readonly byte[] ToBytes()
    {
        ToBytes(out byte[] buffer);
        return buffer;
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
