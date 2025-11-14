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
    /// 前导码 FE FE
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public byte[] Preamble;

    /// <summary>
    /// 起始码 68
    /// </summary>
    public byte StartCode;

    /// <summary>
    /// 地址域 A0-A5（6字节）
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
    public byte[] Address;

    /// <summary>
    /// 起始符 68
    /// </summary>
    public byte FrameStart;

    /// <summary>
    /// 控制码（1字节）
    /// </summary>
    public byte Code;

    /// <summary>
    /// 数据域长度（1字节）
    /// </summary>
    public byte Length;

    /// <summary>
    /// 数据域（动态大小，根据数据域长度决定）最大200字节
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
    public byte[] Data;

    /// <summary>
    /// 校验码（1字节）
    /// </summary>
    public byte Checksum;

    /// <summary>
    /// 结束符 16
    /// </summary>
    public byte EndCode;


    /// <summary>
    /// 构造函数，用于初始化 DLT645 帧的各个字段
    /// </summary>
    /// <param name="address">地址域 A0-A5（6字节）</param>
    /// <param name="control">控制码</param>
    /// <param name="bytes">数据域</param>
    public MessageHeader(byte[] address, byte control, byte[]? bytes)
    {
        this = default;
        Preamble = [0xFE, 0xFE];
        StartCode = 0x68;
        Address = address ?? new byte[6];
        FrameStart = 0x68;
        Code = control;
        Length = (byte)(bytes?.Length ?? 0);
        Data = bytes ?? [];
        Checksum = 0;
        EndCode = 0x16;
    }

    /// <summary>
    /// 从字节数组中反序列化帧
    /// </summary>
    /// <param name="bytes">字节数组</param>
    public MessageHeader(ReadOnlySpan<byte> bytes)
    {
        this = default;
        Preamble = bytes[..2].ToArray();
        StartCode = bytes[2];
        Address = bytes.Slice(3, 6).ToArray();
        FrameStart = bytes[9];
        Code = bytes[10];
        Length = bytes[11];
        Data = bytes.Slice(12, Length).ToArray();
        Checksum = bytes[12 + Length];
        EndCode = bytes[13 + Length];
    }

    /// <summary>
    /// 转换为字节数组
    /// </summary>
    /// <param name="buffer">输出的字节数组缓冲区</param>
    public readonly void ToBytes(out byte[] buffer)
    {
        buffer = _bytePool.Rent(512);

        try
        {
            var span = buffer.AsSpan();

            span[0] = Preamble[0];
            span[1] = Preamble[1];
            span[2] = StartCode;
            span[3] = Address[0];
            span[4] = Address[1];
            span[5] = Address[2];
            span[6] = Address[3];
            span[7] = Address[4];
            span[8] = Address[5];
            span[9] = FrameStart;
            span[10] = Code;
            span[11] = Length;

            Data.CopyTo(span[12..]);

            span[12 + Length] = CalculateChecksum(span[..(12 + Length)]);

            span[13 + Length] = EndCode;
        }
        catch
        {
            _bytePool.Return(buffer);
            throw;
        }
    }

    /// <summary>
    /// 校验和计算方法（此处为简单字节和计算）
    /// </summary>
    /// <param name="data">待计算校验和的数据</param>
    /// <returns>校验和</returns>
    private static byte CalculateChecksum(Span<byte> data)
    {
        byte checksum = 0;
        foreach (var b in data)
        {
            checksum += b;
        }
        return checksum;
    }

    public override readonly bool Equals(object? obj) => obj is MessageHeader other && Equals(other);

    public readonly bool Equals(MessageHeader other)
    {
        return Address.SequenceEqual(other.Address) &&
               Code == other.Code &&
               Length == other.Length &&
               Data.SequenceEqual(other.Data);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Address, Code, Length, Data);
    }

    public static bool operator ==(MessageHeader left, MessageHeader right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MessageHeader left, MessageHeader right)
    {
        return !(left == right);
    }
}
