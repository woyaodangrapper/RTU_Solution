using System.Diagnostics;

namespace Aspdcs.Rtu.TcpServer.Contracts;

public sealed class CircularBuffer
{
    private readonly byte[] buffer;
    private readonly int mask;                // = capacity - 1
    private volatile int headIndex;       // 读指针
    private volatile int tailIndex;       // 写指针

    /// <summary>
    /// capacity 必须是 2 的幂次方，比如 1024、2048、4096……
    /// </summary>
    public CircularBuffer(int capacity)
    {
        if (capacity <= 0 || (capacity & (capacity - 1)) != 0)
            throw new ArgumentException("capacity must be a power of two", nameof(capacity));

        buffer = new byte[capacity];
        mask = capacity - 1;
    }

    /// <summary>
    /// 只读取数据而不移动 headIndex（读指针）
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public byte[] Peek(int length)
    {
        if (length <= 0) return Array.Empty<byte>();

        int available = Count;
        if (length > available) length = available;

        var result = new byte[length];
        int head = headIndex;

        for (int i = 0; i < length; i++)
        {
            int idx = (head + i) & mask;
            result[i] = buffer[idx];
        }

        return result;
    }

    /// <summary>
    /// 可写入的剩余空间
    /// </summary>
    public int FreeCount =>
            // tailIndex - headIndex ≤ capacity
            buffer.Length - (tailIndex - headIndex);

    /// <summary>
    /// 可读取的数据长度
    /// </summary>
    public int Count => tailIndex - headIndex;

    public bool IsEmpty => Count == 0;
    public bool IsFull => Count == buffer.Length;

    /// <summary>
    /// 写入单个字节。返回 true 表示写入成功，false 表示缓冲区已满。
    /// </summary>
    public bool TryWrite(byte value)
    {
        if (Count >= buffer.Length)  // 满时拒写
            return false;

        int idx = tailIndex & mask;
        buffer[idx] = value;

        // 确保 data 写入对消费者可见
        Volatile.Write(ref tailIndex, tailIndex + 1);
        return true;
    }

    /// <summary>
    /// 读取单个字节。返回 true 和 out value 表示读取成功，false 表示缓冲区空。
    /// </summary>
    public bool TryRead(out byte value)
    {
        if (Count <= 0)  // 空时拒读
        {
            value = default;
            return false;
        }

        int idx = headIndex & mask;
        value = buffer[idx];

        // 确保 value 读取对生产者可见
        Volatile.Write(ref headIndex, headIndex + 1);
        return true;
    }

    /// <summary>
    /// 写入多个字节，返回实际写入的长度。
    /// </summary>
    public int Write(ReadOnlySpan<byte> src)
    {
        int written = 0;
        foreach (var b in src)
        {
            if (!TryWrite(b))
                break;
            written++;
        }
        return written;
    }

    /// <summary>
    /// 读取多个字节，返回实际读取的数组（可能小于请求长度）。
    /// </summary>
    public byte[] Read(int length)
    {
        if (length <= 0) return Array.Empty<byte>();

        int available = Count;
        if (length > available) length = available;

        var dst = new byte[length];
        for (int i = 0; i < length; i++)
        {
            TryRead(out dst[i]);
        }
        return dst;
    }

    /// <summary>
    /// 尝试将 src 完整写入环形缓冲区，
    /// 如果容量不足，则阻塞等待或抛出超时异常。
    /// </summary>
    public void WriteBlocking(ReadOnlySpan<byte> src, CancellationToken token, TimeSpan? timeout = null)
    {
        int offset = 0;
        var sw = timeout.HasValue ? Stopwatch.StartNew() : null;

        while (offset < src.Length)
        {
            // 如果超时，抛异常
            if (timeout.HasValue && sw!.Elapsed > timeout.Value)
                throw new TimeoutException("CircularBuffer 写入超时，缓冲区已满。");

            int free = FreeCount;
            if (free == 0)
            {
                Thread.Yield();
                token.ThrowIfCancellationRequested();
                continue;
            }

            // 本次能写入的长度
            int canWrite = Math.Min(free, src.Length - offset);
            for (int i = 0; i < canWrite; i++)
                TryWrite(src[offset + i]);

            offset += canWrite;
        }
    }
}