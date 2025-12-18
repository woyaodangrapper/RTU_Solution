using Aspdcs.Rtu.Contracts;
using Aspdcs.Rtu.TcpClient.Contracts;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace Aspdcs.Rtu.TcpClient;

public abstract class Channel : IDisposable, IContracts
{
    protected CircularBuffer Buffer { get; }
    protected CancellationTokenSource CancellationToken { get; } = new();
    protected ILogger<Channel> Logger { get; }

    internal readonly System.Net.Sockets.TcpClient Listener;

    protected IPAddress IPAddress { get; }

    protected int Port { get; }

    protected Channel([NotNull] ChannelOptions options,
      ILoggerFactory loggerFactory
    )
    {
        Logger = loggerFactory.CreateLogger<Channel>();
        Listener = new System.Net.Sockets.TcpClient();
        Buffer = new CircularBuffer(options.Capacity);
        Port = options.Port;
        IPAddress = options.IPAddress;
    }

    protected virtual bool IsConnected([NotNull] Socket socket)
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