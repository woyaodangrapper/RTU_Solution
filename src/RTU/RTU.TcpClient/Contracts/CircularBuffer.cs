namespace RTU.TcpServer.Contracts;

internal sealed class CircularBuffer
{
    private byte[] buffer;
    private int readIndex;
    private int writeIndex;
    private int count;

    internal CircularBuffer(int capacity)
    {
        buffer = new byte[capacity];
    }

    internal int Capacity => buffer.Length;

    internal int Count => count;

    internal bool IsEmpty => count == 0;

    internal bool IsFull => count == Capacity;


    /// <summary>
    /// 写入数据到环形缓存，支持自动扩容和覆盖旧数据
    /// </summary>
    internal void Write(ReadOnlySpan<byte> source)
    {
        foreach (var b in source)
        {
            if (IsFull)
            {
                ExpandBuffer();
            }

            buffer[writeIndex] = b;
            writeIndex = (writeIndex + 1) % Capacity;

            if (count == Capacity)
            {
                // 缓冲区满了，覆盖：读指针也跟着移动
                readIndex = (readIndex + 1) % Capacity;
            }
            else
            {
                count++;
            }
        }
    }

    /// <summary>
    /// 从环形缓存中读取指定数量的数据
    /// </summary>
    internal byte[] Read(int length)
    {
        if (length <= 0)
            return Array.Empty<byte>();

        if (length > count)
            length = count; // 只读现有的数据

        var result = new byte[length];

        int firstPart = Math.Min(Capacity - readIndex, length);
        Array.Copy(buffer, readIndex, result, 0, firstPart);

        int secondPart = length - firstPart;
        if (secondPart > 0)
            Array.Copy(buffer, 0, result, firstPart, secondPart);

        readIndex = (readIndex + length) % Capacity;
        count -= length;

        return result;
    }

    /// <summary>
    /// 只读取但不移动读指针
    /// </summary>
    internal byte[] Peek(int length)
    {
        if (length <= 0)
            return Array.Empty<byte>();

        if (length > count)
            length = count;

        var result = new byte[length];

        int firstPart = Math.Min(Capacity - readIndex, length);
        Array.Copy(buffer, readIndex, result, 0, firstPart);

        int secondPart = length - firstPart;
        if (secondPart > 0)
            Array.Copy(buffer, 0, result, firstPart, secondPart);

        return result;
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    internal void Clear()
    {
        readIndex = 0;
        writeIndex = 0;
        count = 0;
        Array.Clear(buffer, 0, buffer.Length);
    }

    /// <summary>
    /// 扩容缓存为原容量的两倍
    /// </summary>
    private void ExpandBuffer()
    {
        int newCapacity = Capacity * 2;
        var newBuffer = new byte[newCapacity];

        // 将数据从旧缓冲区复制到新缓冲区
        int firstPart = Math.Min(Capacity - readIndex, count);
        Array.Copy(buffer, readIndex, newBuffer, 0, firstPart);

        int secondPart = count - firstPart;
        if (secondPart > 0)
            Array.Copy(buffer, 0, newBuffer, firstPart, secondPart);

        // 更新指针
        buffer = newBuffer;
        readIndex = 0;
        writeIndex = count;
    }

}