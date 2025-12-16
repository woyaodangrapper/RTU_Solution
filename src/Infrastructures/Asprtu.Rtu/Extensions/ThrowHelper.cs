using System.Runtime.CompilerServices;

namespace Asprtu.Rtu.Extensions;

/// <summary>
/// ArgumentNullException 兼容扩展（用于 netstandard2.1）
/// API 形态对齐 .NET 6+
/// </summary>
static internal class ThrowHelper
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull(object? arg, string? name = null) =>
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(arg, name);
#else
        _ = arg ?? throw new ArgumentNullException(name);
#endif

    public static void ThrowIfNegative(int value, string? paramName = null)
    {
#if NET6_0_OR_GREATER
#pragma warning disable IDE0022 // 使用表达式主体来表示方法
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
#pragma warning restore IDE0022 // 使用表达式主体来表示方法
#else
        if (value < 0)
            throw new ArgumentOutOfRangeException(
                paramName, value, "value must be non-negative");
#endif
    }

    public static void ThrowIfNegative(long value, string? paramName = null)
    {
#if NET6_0_OR_GREATER
#pragma warning disable IDE0022 // 使用表达式主体来表示方法
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
#pragma warning restore IDE0022 // 使用表达式主体来表示方法
#else
        if (value < 0)
            throw new ArgumentOutOfRangeException(
                paramName, value, "value must be non-negative");
#endif
    }

    public static void ThrowIfNullOrWhiteSpace(string? value, string? paramName = null)
    {
#if NET6_0_OR_GREATER
#pragma warning disable IDE0022 // 使用表达式主体来表示方法
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
#pragma warning restore IDE0022 // 使用表达式主体来表示方法
#else
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "value cannot be null or whitespace",
                paramName);
#endif
    }

    public static void ThrowIf(bool condition, object instance)
    {
#if NET6_0_OR_GREATER
#pragma warning disable IDE0022 // 使用表达式主体来表示方法
        ObjectDisposedException.ThrowIf(condition, instance);
#pragma warning restore IDE0022 // 使用表达式主体来表示方法
#else
        if (condition)
            throw new ObjectDisposedException(instance?.ToString() ?? "object");
#endif
    }

    /// <summary>
    /// 如果值小于指定最小值，则抛出 ArgumentOutOfRangeException
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="minValue">最小允许值</param>
    /// <param name="paramName">参数名称</param>
    public static void ThrowIfLessThan(this int value, int minValue, string? paramName = null)
    {
#if NET7_0_OR_GREATER
#pragma warning disable IDE0022 // 使用表达式主体来表示方法
        ArgumentOutOfRangeException.ThrowIfLessThan(value, minValue, paramName);
#pragma warning restore IDE0022 // 使用表达式主体来表示方法
#else
            if (value < minValue)
            {
                throw new ArgumentOutOfRangeException(paramName, value,
                    $"Value must be greater than or equal to {minValue}.");
            }
#endif
    }

    // 可以根据需要再重载 long、double、decimal 等类型
    public static void ThrowIfLessThan(this long value, long minValue, string? paramName = null)
    {
        if (value < minValue)
        {
            throw new ArgumentOutOfRangeException(paramName, value,
                $"Value must be greater than or equal to {minValue}.");
        }
    }
}
