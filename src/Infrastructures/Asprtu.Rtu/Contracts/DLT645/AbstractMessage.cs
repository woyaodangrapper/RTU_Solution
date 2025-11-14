namespace Asprtu.Rtu.Contracts.DLT645;

/// <summary>
/// DLT645 抽象消息基类
/// </summary>
public abstract class AbstractMessage
{
    private byte[] _address = new byte[6];
    private byte[] _data = [];

    /// <summary>
    /// 地址域 A0-A5（6字节）
    /// </summary>
    public virtual ReadOnlySpan<byte> Address => _address;

    /// <summary>
    /// 控制码（1字节）
    /// </summary>
    public virtual byte ControlCode { get; set; }

    /// <summary>
    /// 数据域长度（1字节）
    /// </summary>
    public virtual byte DataLength { get; set; }

    /// <summary>
    /// 数据域
    /// </summary>
    public virtual ReadOnlySpan<byte> Data => _data;

    /// <summary>
    /// 设置地址域
    /// </summary>
    /// <param name="address">地址域数据</param>
    public virtual void SetAddress(ReadOnlySpan<byte> address)
    {
        if (address.Length > 6)
            throw new ArgumentException("地址域长度不能超过6字节", nameof(address));

        _address = new byte[6];
        address.CopyTo(_address);
    }

    /// <summary>
    /// 设置数据域
    /// </summary>
    /// <param name="data">数据域</param>
    public virtual void SetData(ReadOnlySpan<byte> data)
    {
        if (data.Length > 200)
            throw new ArgumentException("数据域长度不能超过200字节", nameof(data));

        _data = data.ToArray();
        DataLength = (byte)data.Length;
    }

    /// <summary>
    /// 序列化为字节数组
    /// </summary>
    /// <returns>序列化后的字节数组</returns>
    public abstract byte[] Serialize();

    /// <summary>
    /// 从字节数组反序列化为消息对象
    /// </summary>
    /// <param name="bytes">字节数组</param>
    protected virtual void Deserialize(byte[] bytes)
    {
        var span = bytes.AsSpan();
        var header = new MessageHeader(span);
        _address = header.Address;
        ControlCode = header.Code;
        DataLength = header.Length;
        _data = header.Data;
    }

    /// <summary>
    /// 从 ReadOnlySpan 反序列化为消息对象
    /// </summary>
    /// <param name="bytes">只读字节序列</param>
    protected virtual void Deserialize(ReadOnlySpan<byte> bytes)
    {
        var header = new MessageHeader(bytes);
        _address = header.Address;
        ControlCode = header.Code;
        DataLength = header.Length;
        _data = header.Data;
    }

    /// <summary>
    /// 计算消息总长度
    /// </summary>
    /// <returns>消息总长度（包括帧头、数据域、校验和结束符）</returns>
    protected virtual int CalculateMessageLength() =>
        // DLT645 消息长度 = 前导码(2) + 起始码(1) + 地址域(6) + 起始符(1) + 控制码(1) + 数据长度(1) + 数据域(N) + 校验码(1) + 结束符(1)
        14 + DataLength;
}