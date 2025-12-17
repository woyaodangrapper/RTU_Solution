using Asprtu.Rtu.DLT645.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace Asprtu.Rtu.DLT645.Serialization;

public class DataDecoder : IDataDecoder
{

    public SemanticValue Decode(ReadOnlySpan<byte> data, [NotNull] DataFormat format)
    {
        return format.Encoding switch
        {
            DataFormats.ValueEncoding.Bcd => DecodeBcd(data, format),
            _ => throw new NotSupportedException($"不支持的编码类型 {format.Encoding}")
        };
    }

    public T Decode<T>(ReadOnlySpan<byte> data, [NotNull] DataFormat format)
        where T : SemanticValue
    {
        return format.Encoding switch
        {
            DataFormats.ValueEncoding.Bcd => (T)(SemanticValue)DecodeBcd(data, format),
            _ => throw new NotSupportedException($"不支持的编码类型 {format.Encoding}")
        };
    }
    private static NumericValue DecodeBcd(ReadOnlySpan<byte> data, DataFormat format)
    {
        if (data.IsEmpty)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        int decimalPlaces = DecimalPlaces(format.Format);

        long value = 0;
        for (int i = data.Length - 1; i >= 0; i--)
        {
            byte high = (byte)((data[i] >> 4) & 0x0F);
            byte low = (byte)(data[i] & 0x0F);

            if (high > 9 || low > 9)
                throw new FormatException($"Invalid BCD value at byte {i}: 0x{data[i]:X2}");

            value = value * 100 + high * 10 + low;
        }

        decimal actualValue = decimalPlaces > 0
            ? value / (decimal)Math.Pow(10, decimalPlaces)
            : value;

        return new(actualValue, format.Unit);
    }


    // 从格式字符串解析小数位
    private static int DecimalPlaces(string format)
    {
        int dotIndex = format.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex < 0)
            return 0;

        return format.Length - dotIndex - 1;
    }
}
