namespace Aspdcs.Rtu.Contracts.Tcp;

public abstract class AbstractMessage
{
    public virtual int Version { get; set; }  // 协议版本，2字节
    public virtual int Reserved { get; set; }  // 预留字段（可做对齐/扩展）
    public virtual int TotalLength { get; set; } = 16; // 消息总长度
    public virtual int CommandId { get; set; }  // 命令ID（2字节）
    public virtual int SequenceId { get; set; }  // 序列号（2字节）

    public abstract byte[] Serialize();

    /// <summary>
    /// 从字节数组反序列化为消息对象
    /// </summary>
    protected virtual void Deserialize(byte[] bytes)
    {
        var span = bytes.AsSpan();
        var header = new MessageHeader(span);
        Version = header.Version;
        Reserved = header.Reserved;
        TotalLength = (int)header.TotalLength;
        CommandId = header.CommandId;
        SequenceId = header.SequenceId;
    }

    protected virtual int CalculateMessageLength() => TotalLength;
}