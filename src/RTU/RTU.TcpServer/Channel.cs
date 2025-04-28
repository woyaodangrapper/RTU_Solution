using Microsoft.Extensions.Logging;
using RTU.TCPServer.Contracts;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RTU.TCPServer;

internal abstract class Channel : IDisposable
{
    protected CircularBuffer Buffer { get; }

    protected ILogger<Channel> Logger { get; }

    internal readonly TcpListener Listener;

    protected Channel(ChannelOptions options,
      ILoggerFactory loggerFactory
    )
    {
        Logger = loggerFactory.CreateLogger<Channel>();
        Listener = new TcpListener(options.IPAddress, options.Port);
        Buffer = new CircularBuffer(options.Capacity);
        Listener.Start();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static long GetPaddedMessageLength(long bodyLength)
    {
        var length = Marshal.SizeOf<MessageHeader>() + bodyLength;

        // Round up to the closest integer divisible by 12. This will add the [padding] if one is needed.
        return 12 * (long)Math.Ceiling(length / 12.0);
    }

    protected long SafeIncrementMessageOffset(long offset, long increment) =>
       (offset + increment) % (Buffer.Capacity * 2);


    public virtual void Dispose()
    {
        Listener.Dispose();
        GC.SuppressFinalize(this);
    }

    public static readonly Action<ILogger, string, Exception?> LogTcpListener =
     LoggerMessage.Define<string>(
         LogLevel.Warning,
         new EventId(1, "TcpListenerPort"),
         "TcpListener Port: {Port}");
}
