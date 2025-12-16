using Asprtu.Rtu.DLT645.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace Asprtu.Rtu.DLT645.Serialization;

public class DataDecoder : IDataDecoder
{
    public object Decode(ReadOnlySpan<byte> data, [NotNull] DataFormat format)
    {
        return format.Encoding switch
        {
            DataFormats.ValueEncoding.Bcd => DecodeBcd(data, format),
            _ => throw new NotSupportedException($"不支持的编码类型 {format.Encoding}")
        };
    }
    private static double DecodeBcd(ReadOnlySpan<byte> data, DataFormat format)
    {
        if (data.IsEmpty)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        int decimalPlaces = ParseDecimalPlaces(format.Format);

        long value = 0;
        for (int i = data.Length - 1; i >= 0; i--) // 逐位表示十进制数
        {
            byte high = (byte)((data[i] >> 4) & 0x0F);
            byte low = (byte)(data[i] & 0x0F);

            if (high > 9 || low > 9)
                throw new FormatException($"Invalid BCD value at byte {i}: 0x{data[i]:X2}");

            value = value * 100 + high * 10 + low;
        }

        if (decimalPlaces > 0)
        {
            double divisor = Math.Pow(10, decimalPlaces);
            return value / divisor;
        }

        return value;
    }

    // 从格式字符串解析小数位
    private static int ParseDecimalPlaces(string format)
    {
        int dotIndex = format.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex < 0)
            return 0;

        return format.Length - dotIndex - 1;
    }
}
