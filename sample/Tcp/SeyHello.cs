using Asprtu.Rtu.Contracts.Tcp;
using System.Text;
using Tcp.Extensions;

namespace Tcp;

public class SayHello : AbstractMessage
{
    public string Name { get; set; } = string.Empty;
    private ulong LocalCurrentTime { get; set; } = (ulong)DateTime.Now.ToTimestamp();

    public override byte[] Serialize()
    {
        var messageBytes = Array.Empty<byte>(); ;

        // 名称
        byte[] nameBytes = Encoding.ASCII.GetBytes(Name.PadLeft(20, ' '));
        Array.Copy(nameBytes, 0, messageBytes, TotalLength, nameBytes.Length);
        TotalLength += nameBytes.Length;

        // 时间戳
        byte[] localCurrentTimeBytes = BitConverter.GetBytes(LocalCurrentTime);
        Array.Copy(localCurrentTimeBytes, 0, messageBytes, TotalLength, localCurrentTimeBytes.Length);
        TotalLength += localCurrentTimeBytes.Length;

        Array.Copy(new byte[10000], 0, messageBytes, TotalLength, 10000);
        TotalLength += 10000;

        return messageBytes;
    }

    protected override int CalculateMessageLength()
    {
        // 名称字段
        int nameLength = Encoding.ASCII.GetByteCount(Name);

        // 发送时间戳
        byte[] localCurrentTimeBytes = BitConverter.GetBytes(LocalCurrentTime);
        return TotalLength + 20 + 10000 + localCurrentTimeBytes.Length;
    }
}