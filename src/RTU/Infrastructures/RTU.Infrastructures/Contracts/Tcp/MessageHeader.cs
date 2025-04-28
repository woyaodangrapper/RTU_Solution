// 我们依赖此结构体的总大小为 12 位。
// 如果你改变结构体的大小，代码中的许多假设将不再有效。
// 这里我们明确指定结构体的大小为 12 字节，并保证字段的通信布局。
using System.Buffers.Binary;
using System.Runtime.InteropServices;
namespace RTU.Infrastructures.Contracts.Tcp;

[StructLayout(LayoutKind.Explicit, Size = 12)] // 结构体总大小为 12 字节，确保与二进制协议或硬件接口兼容
public struct MessageHeader
{
    [FieldOffset(0)]
    public ushort Version; // 协议版本，2字节

    [FieldOffset(2)]
    public ushort Reserved; // 预留字段（可做对齐/扩展）

    [FieldOffset(4)]
    public uint TotalLength; // 消息总长度

    [FieldOffset(8)]
    public ushort CommandId; // 命令ID（2字节）

    [FieldOffset(10)]
    public ushort SequenceId; // 序列号（2字节）


    // 构造函数，用于初始化消息头的各个字段。
    // 在创建 MessageHeader 实例时，必须传入总长度、命令 ID 和序列号。
    public MessageHeader(byte[] bytes)
    {
        this = default;
        Version = BitConverter.ToUInt16(bytes, 0);
        Reserved = BitConverter.ToUInt16(bytes, 2);
        TotalLength = BitConverter.ToUInt32(bytes, 4);
        CommandId = BitConverter.ToUInt16(bytes, 8);
        SequenceId = BitConverter.ToUInt16(bytes, 10);
    }
    public MessageHeader(ReadOnlySpan<byte> bytes)
    {
        this = default;
        Version = BitConverter.ToUInt16(bytes[..2]);
        Reserved = BitConverter.ToUInt16(bytes.Slice(2, 2));
        TotalLength = BitConverter.ToUInt32(bytes.Slice(4, 4));
        CommandId = BitConverter.ToUInt16(bytes.Slice(8, 2));
        SequenceId = BitConverter.ToUInt16(bytes.Slice(10, 2));
    }
    public MessageHeader(int version, int reserved, int totalLength, int commandId, int sequenceId)
    {
        Version = (ushort)version;
        Reserved = (ushort)reserved;
        TotalLength = (uint)totalLength;
        CommandId = (ushort)commandId;
        SequenceId = (ushort)sequenceId;
    }
    // 序列化为字节数组
    public readonly byte[] ToBytes(bool? isBigEndian = null)
    {
        bool isBigEndianFlag = isBigEndian ?? !BitConverter.IsLittleEndian;
        byte[] buffer = new byte[12];
        if (isBigEndianFlag)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0), Version);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2), Reserved);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(4), TotalLength);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(8), CommandId);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(10), SequenceId);
        }
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(0), Version);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(2), Reserved);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(4), TotalLength);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(8), CommandId);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(10), SequenceId);
        }

        return buffer;
    }
}
/*
    Offset |     字段       | 大小 | 用途
    -------| --------------| ------| ------------------------
    0      |   Version     | 2B    | 协议版本
    2      |   Reserved    | 2B    | 预留 / 对齐用途
    4      |   TotalLength | 4B    | 消息总长度（含包头 + 包体）
    8      |   CommandId   | 2B    | 命令标识
    10     |   SequenceId  | 2B    | 消息序号
*/