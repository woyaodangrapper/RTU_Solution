using Asprtu.Rtu.Attributes;
using Asprtu.Rtu.Contracts.Tcp;
using Asprtu.Rtu.Extensions.Tcp;
using Asprtu.Rtu.TcpClient.Contracts;
using Asprtu.Rtu.TcpClient.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Asprtu.Rtu.TcpClient;

[LibraryCapacities]
public sealed class TcpClient : Channel, ITcpClient
{
    public System.Net.Sockets.TcpClient Client => Listener;

    public Action<Exception>? OnError { get; set; }
    public Action<System.Net.Sockets.TcpClient>? OnSuccess { get; set; }
    public Action<System.Net.Sockets.TcpClient, byte[]>? OnMessage { get; set; }

    public TcpClient() : base(new("default"), NullLoggerFactory.Instance)
    {
    }

    [ActivatorUtilitiesConstructor]
    public TcpClient(ILoggerFactory loggerFactory) : base(new("default"), loggerFactory)
        => _tracker.SetState(ConnectionState.Listening);

    public TcpClient(ChannelOptions options, ILoggerFactory loggerFactory) : base(options, loggerFactory)
        => _tracker.SetState(ConnectionState.Listening);

    private readonly ConnectionStateTracker _tracker = new();

    public TcpInfo TcpInfo => _tracker.GetSnapshot(
      Client.Client.RemoteEndPoint as IPEndPoint,
      Client.Client.LocalEndPoint as IPEndPoint
    );

    public async Task<bool> TrySendAsync(int data) =>
        await TryWriteAsync(BitConverter.GetBytes(data)).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(float data) =>
        await TryWriteAsync(ByteConverter.GetBytes(data)).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(double data) =>
        await TryWriteAsync(ByteConverter.GetBytes(data)).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(bool data) =>
        await TryWriteAsync(ByteConverter.GetBytes(data)).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(short data) =>
        await TryWriteAsync(ByteConverter.GetBytes(data)).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(long data) =>
        await TryWriteAsync(ByteConverter.GetBytes(data)).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(byte data) =>
        await TryWriteAsync([data]).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(char data) =>
        await TryWriteAsync(ByteConverter.GetBytes(data)).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(decimal data) =>
        await TryWriteAsync(ByteConverter.GetBytes(data)).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(string data) =>
        await TryWriteAsync(ByteConverter.GetBytes(data)).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(DateTime data) =>
        await TryWriteAsync(ByteConverter.GetBytes(data)).ConfigureAwait(false);

    public async Task<bool> TrySendAsync<T>([NotNull] T data) where T : AbstractMessage, new() =>
        await TryWriteAsync(data.Serialize()).ConfigureAwait(false);

    public async Task<bool> TryWriteAsync(byte[] bytes)
    {
        var header = new MessageHeader(length => length + bytes.Length);

        header.ToBytes(out byte[] headerBytes);
        bytes = [.. headerBytes, .. bytes];
        try
        {
            _tracker.AddSent(bytes?.Length ?? 0);
            var stream = Listener.GetStream();
            await stream.WriteAsync(bytes).ConfigureAwait(false);
        }
        catch (OperationCanceledException e)
        {
            LogTcpListener(Logger, "Failed to send data", e);
            OnError?.Invoke(e);
            return false;
        }
        catch (SocketException e)
        {
            LogTcpListener(Logger, "SocketException exception in TrySend", e);
            OnError?.Invoke(e);
            return false;
        }
        return true;
    }

    private bool TryAssemble(ReadOnlyMemory<byte> data, out byte[] Integrity)
    {
        Integrity = default!;
        Buffer.Write(data.Span);
        int headerSize = Marshal.SizeOf<MessageHeader>();
        if (Buffer.Count < headerSize)
        {
            return false;
        }
        byte[] headerBytes = Buffer.Peek(headerSize);
        if (headerBytes.Length < headerSize)
        {
            return false;
        }
        var header = headerBytes.TryReadHeader();
        if (!header.HasValue)
        {
            return false;
        }
        if (Buffer.Count < header.Value.TotalLength)
        {
            return false;
        }
        Integrity = Buffer.Read((int)header.Value.TotalLength);
        return true;
    }

    private void ReadLoop(System.Net.Sockets.TcpClient client, CancellationToken stoppingToken)
    {
        try
        {
            using var stream = client.GetStream();

            var buffer = new byte[1024];

            int bytesRead = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                bytesRead = stream.Read(buffer);
                if (bytesRead == 0)
                    break; // Client disconnected

                if (TryAssemble(buffer.AsMemory(0, bytesRead), out var Integrity))
                {
                    _tracker.AddReceived(Integrity.LongLength);
                    OnMessage?.Invoke(client, Integrity);
                }
            }
        }
        catch (OperationCanceledException e)
        {
            LogTcpListener(Logger, "the task has been canceled", e);
            OnError?.Invoke(e);
        }
        finally
        {
            client.Close();
        }
    }

    public async Task TryExecuteAsync()
    {
        _tracker.SetState(ConnectionState.Connecting);

        try
        {
            if (!Listener.Connected)
            {
                await Listener.ConnectAsync(new IPEndPoint(IPAddress, Port)).ConfigureAwait(false);
            }
            OnSuccess?.Invoke(Listener);
            _tracker.SetState(ConnectionState.Active);

            while (!CancellationToken.IsCancellationRequested)
            {
                var cancellationToken = CancellationToken.Token;
                ReadLoop(Listener, cancellationToken);
            }
        }
        catch (OperationCanceledException e)
        {
            LogTcpListener(Logger, "mission canceled", e);
            OnError?.Invoke(e);
        }
        catch (SocketException e)
        {
            LogTcpListener(Logger, "SocketException exception in TryExecuteAsync", e);
            OnError?.Invoke(e);
        }
        _tracker.SetState(ConnectionState.Closed);
    }

    protected override void Dispose(bool disposing)
    {
        _tracker.SetState(ConnectionState.Closing);
        base.Dispose(disposing);
    }
}