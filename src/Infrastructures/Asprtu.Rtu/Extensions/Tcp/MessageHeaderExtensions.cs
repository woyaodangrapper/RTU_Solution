using Asprtu.Rtu.Contracts.Tcp;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Asprtu.Rtu.Extensions.Tcp;

public static class MessageHeaderExtensions
{
    private static MessageHeader ReadHeader(ReadOnlySpan<byte> header)
        => new(header);

    private static MessageHeader ReadHeader(byte[] header)
        => new(header);

    public static MessageHeader? TryReadHeader(this NetworkStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            byte[] header = new byte[Marshal.SizeOf<MessageHeader>()];
            int bytesRead = stream.Read(header, 0, header.Length);
            if (bytesRead != header.Length)
                return null;
            return ReadHeader(header);
        }
        catch (IOException) // Catch specific exception for stream read errors
        {
            return null;
        }
        catch (ObjectDisposedException) // Catch specific exception for disposed stream
        {
            return null;
        }
    }

    public static MessageHeader? TryReadHeader(this byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        try
        {
            byte[] header = bytes[0..Marshal.SizeOf<MessageHeader>()];
            return ReadHeader(header);
        }
        catch (ArgumentOutOfRangeException) // Catch specific exception for slicing errors
        {
            return null;
        }
    }

    public static MessageHeader? TryReadHeader(this ReadOnlyMemory<byte> bytes)
    {
        try
        {
            var header = bytes.Span[..Marshal.SizeOf<MessageHeader>()];
            return ReadHeader(header);
        }
        catch (ArgumentOutOfRangeException) // Catch specific exception for slicing errors
        {
            return null;
        }
    }

    public static MessageHeader? TryReadHeader(this List<byte> bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var HeaderSize = Marshal.SizeOf<MessageHeader>();
        if (bytes.Count < HeaderSize)
        {
            return null; // Not enough bytes to parse a complete header
        }

        try
        {
            var headerBytes = bytes.GetRange(0, HeaderSize).ToArray();
            return ReadHeader(headerBytes);
        }
        catch (ArgumentException) // Catch specific exception for GetRange errors
        {
            return null;
        }
    }
}