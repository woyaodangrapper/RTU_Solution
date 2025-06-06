// 我们依赖此结构体的总大小为 16 位。
// 如果你改变结构体的大小，代码中的许多假设将不再有效。
// 这里我们明确指定结构体的大小为 16 字节，并保证字段的通信布局。
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Asprtu.Rtu.Contracts.Tcp;

[StructLayout(LayoutKind.Explicit, Size = 16)] // 结构体总大小为 16 字节，确保与二进制协议或硬件接口兼容
public struct MessageHeader : IEquatable<MessageHeader>
{
    private static readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

    [FieldOffset(0)]
    public ushort Version; // 协议版本，2字节

    [FieldOffset(2)]
    public ushort Reserved; // 预留字段（可做对齐/扩展）

    [FieldOffset(8)]
    public uint TotalLength; // 消息总长度（4字节）

    [FieldOffset(12)]
    public ushort CommandId; // 命令ID（2字节）

    [FieldOffset(14)]
    public ushort SequenceId; // 序列号（2字节）

    // 构造函数，用于初始化消息头的各个字段。
    // 在创建 MessageHeader 实例时，必须传入总长度、命令 ID 和序列号。
    public MessageHeader(byte[] bytes)
    {
        this = default;

        var span = bytes.AsSpan();
        Version = BitConverter.ToUInt16(span[..2]);           // 偏移量0，读取2字节
        Reserved = BitConverter.ToUInt16(span[2..4]);         // 偏移量2，读取2字节
        TotalLength = BitConverter.ToUInt32(span[8..12]);     // 偏移量8，跳过4字节未用区域，读取4字节
        CommandId = BitConverter.ToUInt16(span[12..14]);      // 偏移量12，读取2字节
        SequenceId = BitConverter.ToUInt16(span[14..16]);     // 偏移量14，读取2字节
    }

    public MessageHeader(ReadOnlySpan<byte> bytes)
    {
        this = default;
        Version = BitConverter.ToUInt16(bytes[..2]);           // 偏移量0，读取2字节
        Reserved = BitConverter.ToUInt16(bytes[2..4]);         // 偏移量2，读取2字节
        TotalLength = BitConverter.ToUInt32(bytes[8..12]);     // 偏移量8，跳过4字节未用区域，读取4字节
        CommandId = BitConverter.ToUInt16(bytes[12..14]);      // 偏移量12，读取2字节
        SequenceId = BitConverter.ToUInt16(bytes[14..16]);     // 偏移量14，读取2字节
    }

    public MessageHeader(int totalLength) => TotalLength = (uint)totalLength;

    public MessageHeader(Func<int, int> totalLengthProvider)
    {
        ArgumentNullException.ThrowIfNull(totalLengthProvider);
        TotalLength = (uint)totalLengthProvider(Marshal.SizeOf<MessageHeader>());
    }

    public MessageHeader(int version, int reserved, int totalLength, int commandId, int sequenceId)
    {
        Version = (ushort)version;
        Reserved = (ushort)reserved;
        TotalLength = (uint)totalLength;
        CommandId = (ushort)commandId;
        SequenceId = (ushort)sequenceId;
    }

    /// <summary>
    /// 序列化为字节数组
    /// </summary>
    /// <param name="isBig">是大端还是小端，不填则默认使用系统配置</param>
    /// <param name="buffer">以out返回的消息头字节数组</param>
    /// <returns></returns>

    public readonly void ToBytes(out byte[] buffer, bool? isBig = null)
    {
        bool isBigEndianFlag = isBig ?? !BitConverter.IsLittleEndian;

        // 从池中租用一个字节数组,请求一个大小为 12 的数组 (一个标准块大小，可能是 16、32、64……等)
        buffer = _bytePool.Rent(12);

        try
        {// 1 step 392,700 ns ， 2 step 4,500 ns
            var span = buffer.AsSpan();
            if (isBigEndianFlag)
            {
                BinaryPrimitives.WriteUInt16BigEndian(span[..2], Version);    // 偏移量0，写入2字节
                BinaryPrimitives.WriteUInt16BigEndian(span[2..4], Reserved);  // 偏移量2，写入2字节
                BinaryPrimitives.WriteUInt32BigEndian(span[8..12], TotalLength); // 偏移量8，跳过4字节未用区域，写入4字节
                BinaryPrimitives.WriteUInt16BigEndian(span[12..14], CommandId); // 偏移量12，写入2字节
                BinaryPrimitives.WriteUInt16BigEndian(span[14..16], SequenceId); // 偏移量14，写入2字节
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(span[..2], Version);    // 偏移量0，写入2字节
                BinaryPrimitives.WriteUInt16LittleEndian(span[2..4], Reserved);  // 偏移量2，写入2字节
                BinaryPrimitives.WriteUInt32LittleEndian(span[8..12], TotalLength); // 偏移量8，跳过4字节未用区域，写入4字节
                BinaryPrimitives.WriteUInt16LittleEndian(span[12..14], CommandId); // 偏移量12，写入2字节
                BinaryPrimitives.WriteUInt16LittleEndian(span[14..16], SequenceId); // 偏移量14，写入2字节
            }
        }
        catch
        {
            // 确保在发生异常时返回字节数组给池中
            _bytePool.Return(buffer);
            throw;
        }
    }

    public override readonly bool Equals(object? obj) // 修改为 object? 以匹配为 Null 性
        => obj is MessageHeader other && Equals(other);

    public readonly bool Equals(MessageHeader other)
    {
        return Version == other.Version &&
               Reserved == other.Reserved &&
               TotalLength == other.TotalLength &&
               CommandId == other.CommandId &&
               SequenceId == other.SequenceId;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Version, Reserved, TotalLength, CommandId, SequenceId);
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

// 4 - 7 <未用>  4B   保留间隙
/*
    Offset |     字段       | 大小 | 用途
    -------| --------------| ------| ------------------------
    0      |   Version     | 2B    | 协议版本
    2      |   Reserved    | 2B    | 预留 / 对齐用途
    4      |   TotalLength | 4B    | 消息总长度（含包头 + 包体）
    8      |   CommandId   | 2B    | 命令标识
    10     |   SequenceId  | 2B    | 消息序号
*/