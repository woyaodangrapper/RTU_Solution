using Asprtu.Rtu.Contracts.DLT645;

namespace Asprtu.Rtu.DLT645.Extensions;

public static class MessageHeaderExtensions
{
    private static MessageHeader ReadHeader(ReadOnlySpan<byte> header)
        => new(header);

    private static MessageHeader ReadHeader(byte[] header)
        => new(header);
    public static MessageHeader? TryReadHeader(this byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        throw new ArgumentException(nameof(bytes));
    }
}