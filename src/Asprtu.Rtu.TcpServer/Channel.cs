using Asprtu.Rtu.Contracts.Tcp;
using Asprtu.Rtu.TcpServer.Contracts;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Asprtu.Rtu.TcpServer;

public abstract class Channel : IDisposable
{
    protected CircularBuffer Buffer { get; }
    protected CancellationTokenSource CancellationToken { get; } = new();

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

    protected virtual bool IsConnected(Socket socket)
     => !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);

    public static readonly Action<ILogger, string, Exception?> LogTcpListener =
     LoggerMessage.Define<string>(
         LogLevel.Warning,
         new EventId(1, "TcpListenerPort"),
         "TcpListener Port: {Port}");

    private bool _disposed;

    // Existing fields and methods...

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                Listener.Dispose();
                CancellationToken.Dispose();
            }

            // Dispose unmanaged resources if any
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Channel()
    {
        Dispose(false);
    }
}