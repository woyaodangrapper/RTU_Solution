using Aspdcs.Rtu.Contracts.DLT645;
using ThrowHelper = Aspdcs.Rtu.Extensions.ThrowHelper;

namespace Aspdcs.Rtu.DLT645.Extensions;

internal static class MessageHeaderExtensions
{
    /// <summary>
    /// +0x33
    /// </summary>
    /// <param name="data"></param>
    public static void EncodeData(Span<byte> data)
    {
        for (int i = 0; i < data.Length; i++)
            data[i] += 0x33;
    }

    /// <summary>
    /// -0x33
    /// </summary>
    /// <param name="data"></param>
    public static void DecodeData(Span<byte> data)
    {
        for (int i = 0; i < data.Length; i++)
            data[i] -= 0x33;
    }

    /// <summary>
    /// 重新计算并更新 DLT645 帧的校验和（用于加密数据后）
    /// </summary>
    /// <param name="frame">完整的 DLT645 帧</param>
    public static void UpdateChecksum(Span<byte> frame)
    {
        // 跳过前导符 FE
        int startCodeIndex = 0;
        while (startCodeIndex < frame.Length && frame[startCodeIndex] == 0xFE)
            startCodeIndex++;

        if (frame.Length - startCodeIndex < 12)
            return; // 帧太短

        if (frame[startCodeIndex] != 0x68)
            return; // 无效帧

        byte dataLength = frame[startCodeIndex + 9];
        int checksumIndex = startCodeIndex + 10 + dataLength; // 校验和位置

        if (checksumIndex >= frame.Length)
            return; // 无效长度

        // 计算校验和：从第一个 68 到数据域末尾
        byte checksum = 0;
        for (int i = startCodeIndex; i < checksumIndex; i++)
            checksum += frame[i];

        // 更新校验和
        frame[checksumIndex] = checksum;
    }



    /// <summary>
    /// 判断是否为 DL/T645 广播帧（地址域 6 字节全为 0xAA）
    /// </summary>
    public static bool IsBroadcast(this ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 10)
            return false;
        // 广播地址字节
        ReadOnlySpan<byte> broadcast = [0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA];
        for (int i = 0; i <= frame.Length - 6; i++)
        {
            if (frame.Slice(i, 6).SequenceEqual(broadcast))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试从字节数组中提取完整的 DLT645 帧（处理半包/粘包）
    /// </summary>
    /// <param name="bytes">待解析的字节数组</param>
    /// <param name="header">输出解析出的帧头</param>
    /// <param name="consumedBytes">输出已消费的字节数（用于粘包处理）</param>
    /// <returns>帧结构是否无效</returns>
    public static bool TryReadHeader(this byte[] bytes, out MessageHeader? header, out int consumedBytes)
    {
        ThrowHelper.ThrowIfNull(bytes);
        return TryReadHeader(bytes.AsSpan(), out header, out consumedBytes);
    }

    /// <summary>
    /// 尝试从字节数组中提取完整的 DLT645 帧（简化版，不返回消费字节数）
    /// </summary>
    public static MessageHeader? TryReadHeader(this byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(bytes);
        return TryReadHeader(bytes.AsSpan(), out var header, out _) ? header : null;
    }

    /// <summary>
    /// 尝试从 Span 中提取完整的 DLT645 帧（核心实现）
    /// </summary>
    /// <param name="span">待解析的字节序列</param>
    /// <param name="header">输出解析出的帧头</param>
    /// <param name="consumedBytes">输出已消费的字节数（包含前导 FE 和完整帧）</param>
    /// <returns>帧结构是否无效</returns>
    public static bool TryReadHeader(this ReadOnlySpan<byte> span, out MessageHeader? header, out int consumedBytes)
    {
        header = null;
        consumedBytes = 0;

        int offset = 0;
        while (offset < span.Length && span[offset] == 0xFE)
            offset++;

        if (span.Length - offset < 12)
            return false; // 半包：数据不足

        if (span[offset] != 0x68)
        {
            consumedBytes = offset + 1;
            return false;
        }

        if (span[offset + 7] != 0x68)
        {
            consumedBytes = offset + 1;
            return false;
        }

        byte dataLength = span[offset + 9];

        int totalFrameLength = 12 + dataLength;

        if (span.Length - offset < totalFrameLength)
            return false; // 半包：数据长度字段指示的完整帧尚未到达

        var frameSpan = span.Slice(offset, totalFrameLength);

        if (frameSpan[^1] != 0x16)
        {
            consumedBytes = offset + 1;
            return false;
        }

        byte expectedChecksum = frameSpan[^2];
        byte actualChecksum = 0;
        for (int i = 0; i < frameSpan.Length - 2; i++)
            actualChecksum += frameSpan[i];

        if (actualChecksum != expectedChecksum)
        {
            consumedBytes = offset + 1;
            return false;
        }

        try
        {
            byte[] preamble = offset > 0 ? span.Slice(0, offset).ToArray() : [];
            header = new MessageHeader(frameSpan);
            consumedBytes = offset + totalFrameLength; // 前导 + 完整帧
            return true;
        }
        catch
        {
            // 构造失败
            consumedBytes = offset + 1;
            return false;
        }
    }

    /// <summary>
    /// 快速从字节序列中提取 DLT645 数据域（不对整个帧验证）
    /// </summary>
    /// <param name="bytes">包含完整 DLT645 帧的字节数组</param>
    /// <param name="data">输出数据域</param>
    /// <returns>帧结构是否无效</returns>
    public static bool TryGetData(this byte[] bytes, out Span<byte> data)
    {
        ThrowHelper.ThrowIfNull(bytes);

        // 假设内部有从 bytes 中解析 Data 的逻辑，返回 span
        bool valid = TryGetData(bytes.AsSpan(), out var span);
        data = valid ? span : [];
        return valid;
    }

    /// <summary>
    /// 快速从字节序列中提取 DLT645 数据域
    /// </summary>
    /// <param name="span">包含完整 DLT645 帧的字节序列</param>
    /// <param name="data">输出数据域</param>
    /// <returns>帧结构是否无效</returns>
    public static bool TryGetData(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> data)
    {
        data = default;

        int startCodeIndex = 0;
        while (startCodeIndex < span.Length && span[startCodeIndex] == 0xFE)
            startCodeIndex++;

        if (span.Length - startCodeIndex < 12)
            return false;

        if (span[startCodeIndex] != 0x68)
            return false;

        if (span[startCodeIndex + 7] != 0x68)
            return false;

        byte dataLength = span[startCodeIndex + 9];

        int totalFrameLength = 12 + dataLength;
        if (span.Length - startCodeIndex < totalFrameLength)
            return false;

        // 验证结束码
        if (span[startCodeIndex + totalFrameLength - 1] != 0x16)
            return false;

        // 提取数据域
        int dataStartIndex = startCodeIndex + 10;
        data = span.Slice(dataStartIndex, dataLength);

        return true;
    }
    /// <summary>
    /// 快速从字节序列中提取 DLT645 数据域
    /// </summary>
    /// <param name="span">包含完整 DLT645 帧的字节序列</param>
    /// <param name="data">输出数据域</param>
    /// <returns>帧结构是否无效</returns>
    public static bool TryGetData(this Span<byte> span, out Span<byte> data)
    {
        data = default;

        int startCodeIndex = 0;
        while (startCodeIndex < span.Length && span[startCodeIndex] == 0xFE)
            startCodeIndex++;

        if (span.Length - startCodeIndex < 12)
            return false;

        if (span[startCodeIndex] != 0x68)
            return false;

        if (span[startCodeIndex + 7] != 0x68)
            return false;

        byte dataLength = span[startCodeIndex + 9];

        int totalFrameLength = 12 + dataLength;
        if (span.Length - startCodeIndex < totalFrameLength)
            return false;

        // 验证结束码
        if (span[startCodeIndex + totalFrameLength - 1] != 0x16)
            return false;

        // 提取数据域
        int dataStartIndex = startCodeIndex + 10;
        data = span.Slice(dataStartIndex, dataLength);

        return true;
    }

    /// <summary>
    /// 快速从 Memory 中提取 DLT645 数据域
    /// </summary>
    /// <param name="memory">包含完整 DLT645 帧的内存</param>
    /// <param name="data">输出数据域</param>
    /// <returns>帧结构是否无效</returns>
    public static bool TryGetData(this Memory<byte> memory, out Memory<byte> data)
    {
        data = default;
        Span<byte> span = memory.Span;

        int startCodeIndex = 0;
        while (startCodeIndex < span.Length && span[startCodeIndex] == 0xFE)
            startCodeIndex++;

        if (span.Length - startCodeIndex < 12)
            return false;

        if (span[startCodeIndex] != 0x68)
            return false;

        if (span[startCodeIndex + 7] != 0x68)
            return false;

        byte dataLength = span[startCodeIndex + 9];

        int totalFrameLength = 12 + dataLength;
        if (span.Length - startCodeIndex < totalFrameLength)
            return false;

        // 验证结束码
        if (span[startCodeIndex + totalFrameLength - 1] != 0x16)
            return false;

        // 提取数据域
        int dataStartIndex = startCodeIndex + 10;
        data = memory.Slice(dataStartIndex, dataLength);

        return true;
    }
}