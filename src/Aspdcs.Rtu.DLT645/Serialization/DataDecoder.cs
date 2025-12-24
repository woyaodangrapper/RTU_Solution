using Aspdcs.Rtu.Contracts.DLT645;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspdcs.Rtu.DLT645.Serialization;

public class DataDecoder : IDataDecoder
{
    /*
        68 11 11 00 00 00 00 68 11 04 33 33 34 33 D4 16
        │                    │  │  │  └─────────┘ │  │
        │                    │  │  │      │       │  └─ 结束符 16H
        │                    │  │  │      │       └──── 校验和 D4H
        │                    │  │  │      └──────────── 加密数据域(4字节)
        │                    │  │  └─────────────────── 数据长度 04H
        │                    │  └────────────────────── 控制码 11H(读数据)
        │                    └───────────────────────── 起始符 68H
        └────────────────────────────────────────────── 地址 68 11 11 00 00 00 00
     */
    public SemanticValue Decode(ReadOnlySpan<byte> message, [NotNull] DataFormat format)
    {
        message.TryGetData(out var data);
        return format.Encoding switch
        {
            DataFormats.ValueEncoding.Bcd => DecodeBcd(data, format),
            _ => throw new NotSupportedException($"不支持的编码类型 {format.Encoding}")
        };
    }

    public T Decode<T>(ReadOnlySpan<byte> message, [NotNull] DataFormat format)
        where T : SemanticValue
    {
        message.TryGetData(out var data);
        return format.Encoding switch
        {
            DataFormats.ValueEncoding.Bcd => (T)(SemanticValue)DecodeBcd(data, format),
            _ => throw new NotSupportedException($"不支持的编码类型 {format.Encoding}")
        };
    }

    public async IAsyncEnumerable<SemanticValue> TryDecodeAsync(
      IAsyncEnumerable<MessageHeader> source,
      DataFormat format,
      Action<Exception>? onError = null,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var header in source.WithCancellation(cancellationToken))
        {
            SemanticValue value = Decode(header.ToBytes(), format);
            yield return value;
        }
    }

    public async IAsyncEnumerable<T> TryDecodeAsync<T>(
        IAsyncEnumerable<MessageHeader> source,
        DataFormat format,
        Action<Exception>? onError = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : SemanticValue
    {
        await foreach (var header in source.WithCancellation(cancellationToken))
        {
            T value = Decode<T>(header.ToBytes(), format);
            yield return value;
        }
    }

    private static NumericValue DecodeBcd(ReadOnlySpan<byte> data, DataFormat format)
    {
        if (data.IsEmpty)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        // data => [00 00 01 00 | 00 00 00 00 | 01] 最后一位是多出来的，非协议标准字符。

        ReadOnlySpan<byte> idSegment = data[..4]; // 标识符域
        ReadOnlySpan<byte> valueSegment = data.Slice(4, format.Length); // 数值域
        ReadOnlySpan<byte> customSegment = data[(4 + format.Length)..]; // 自定义域


        long numericValue = 0;// 高低位翻转遍历
        for (int i = valueSegment.Length - 1; i >= 0; i--)
        {
            byte b = valueSegment[i];

            byte high = (byte)((b >> 4) & 0x0F);
            byte low = (byte)(b & 0x0F);
            if (high > 9 || low > 9)
                continue;

            numericValue = numericValue * 100 + high * 10 + low;
        }

        decimal physicalValue =
            numericValue * (decimal)Math.Pow(10, format.Exponent);

        Span<byte> reversed = stackalloc byte[idSegment.Length];
        for (int i = 0; i < idSegment.Length; i++)
            reversed[i] = idSegment[idSegment.Length - 1 - i];
        // 转成十六进制字符串方便展示标识符和厂家自定义
#if NET6_0_OR_GREATER
        string id = Convert.ToHexString(reversed);
        string custom = Convert.ToHexString(customSegment);
#else
        string id = BitConverter.ToString(reversed.ToArray()).Replace("-", "");
        string custom = BitConverter.ToString(customSegment.ToArray()).Replace("-", "");
#endif

        return new(id, physicalValue, format.Format, format.Unit, custom);
    }

}