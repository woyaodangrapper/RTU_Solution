using Asprtu.Rtu.Extensions;
using System.Text;

namespace System;

public static class ByteConverter
{
    /// <summary>
    /// 通用对象转字节数组
    /// </summary>
    public static byte[] GetBytes(object obj)
    {
        ThrowHelper.ThrowIfNull(obj);
        return obj switch
        {
            int i => BitConverter.GetBytes(i),
            float f => BitConverter.GetBytes(f),
            double d => BitConverter.GetBytes(d),
            bool b => BitConverter.GetBytes(b),
            short s => BitConverter.GetBytes(s),
            long l => BitConverter.GetBytes(l),
            byte bt => [bt],
            char c => BitConverter.GetBytes(c),
            decimal dec => DecimalToBytes(dec),
            string str => Encoding.UTF8.GetBytes(str),
            DateTime dt => BitConverter.GetBytes(dt.ToBinary()),
            _ => throw new NotSupportedException($"Type {obj.GetType()} is not supported.")
        };
    }

    /// <summary>
    /// 字节数组转通用对象
    /// </summary>
    public static T GetObject<T>(byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(bytes);
        bytes = bytes[16..];
        if (typeof(T) == typeof(int)) return (T)(object)BitConverter.ToInt32(bytes, 0);
        if (typeof(T) == typeof(float)) return (T)(object)BitConverter.ToSingle(bytes, 0);
        if (typeof(T) == typeof(double)) return (T)(object)BitConverter.ToDouble(bytes, 0);
        if (typeof(T) == typeof(bool)) return (T)(object)BitConverter.ToBoolean(bytes, 0);
        if (typeof(T) == typeof(short)) return (T)(object)BitConverter.ToInt16(bytes, 0);
        if (typeof(T) == typeof(long)) return (T)(object)BitConverter.ToInt64(bytes, 0);
        if (typeof(T) == typeof(byte)) return (T)(object)bytes[0]; // 单字节直接返回
        if (typeof(T) == typeof(char)) return (T)(object)BitConverter.ToChar(bytes, 0);
        if (typeof(T) == typeof(decimal)) return (T)(object)BytesToDecimal(bytes);
        if (typeof(T) == typeof(string)) return (T)(object)Encoding.UTF8.GetString(bytes);
        if (typeof(T) == typeof(DateTime)) return (T)(object)DateTime.FromBinary(BitConverter.ToInt64(bytes, 0));

        throw new NotSupportedException($"Type {typeof(T)} is not supported.");
    }

    private static byte[] DecimalToBytes(decimal dec)
    {
        var bits = decimal.GetBits(dec);
        byte[] result = new byte[bits.Length * sizeof(int)];
        for (int i = 0; i < bits.Length; i++)
        {
            Array.Copy(BitConverter.GetBytes(bits[i]), 0, result, i * sizeof(int), sizeof(int));
        }
        return result;
    }

    private static decimal BytesToDecimal(byte[] bytes)
    {
        if (bytes.Length != 16)
            throw new ArgumentException("Decimal bytes array must have 16 elements.", nameof(bytes));

        int[] bits = new int[4];
        for (int i = 0; i < 4; i++)
        {
            bits[i] = BitConverter.ToInt32(bytes, i * 4);
        }

        return new decimal(bits);
    }
}