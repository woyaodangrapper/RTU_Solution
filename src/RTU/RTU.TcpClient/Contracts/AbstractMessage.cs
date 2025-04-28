namespace RTU.TCPClient.Contracts;

public abstract class AbstractMessage
{

    public virtual int Version { get; set; } = 0; // 协议版本，2字节
    public virtual int Reserved { get; set; } = 0; // 预留字段（可做对齐/扩展）
    public virtual int TotalLength { get; set; } = 12; // 消息总长度
    public virtual int CommandId { get; set; } = 0; // 命令ID（2字节）
    public virtual int SequenceId { get; set; } = 0; // 序列号（2字节）

    public virtual byte[] Serialize()
    {
        return Serialize(false);
    }

    protected virtual byte[] Serialize(bool isBigEndian)
    {
        // 计算报文长度
        int messageLength = CalculateMessageLength();
        // 构造报文内容
        byte[] messageBytes = new byte[messageLength];

        byte[] baseBytes = new MessageHeader(Version, Reserved, messageLength, CommandId, SequenceId).ToBytes();

        Array.Copy(baseBytes, 0, messageBytes, 0, baseBytes.Length);


        return messageBytes;
    }


    protected virtual int CalculateMessageLength()
    {
        return TotalLength;
    }

}
