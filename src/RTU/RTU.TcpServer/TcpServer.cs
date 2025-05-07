using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RTU.Infrastructures.Contracts.Tcp;
using RTU.Infrastructures.Extensions.Tcp;
using RTU.TcpServer.Contracts;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace RTU.TcpServer;

internal sealed class TcpServer : Channel, ITcpServer
{
    public TcpListener Server => Listener;

    public Action<Exception>? OnError { get; set; }
    public Action<TcpListener>? OnSuccess { get; set; }
    public Action<TcpListener, TcpClient, byte[]>? OnMessage { get; set; }

    private readonly ConcurrentDictionary<string, TcpClient> _clients = new();

    public TcpServer() : base(new("default"), NullLoggerFactory.Instance) { }

    public TcpServer(ChannelOptions options, ILoggerFactory loggerFactory)
        : base(options, loggerFactory) => OnSuccess?.Invoke(Listener);
    public async Task<bool> TrySendAsync(int data, TcpClient? client = null) =>
        await TryWriteAsync(ByteConverter.GetBytes(data), client).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(float data, TcpClient? client = null) =>
        await TryWriteAsync(ByteConverter.GetBytes(data), client).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(double data, TcpClient? client = null) =>
        await TryWriteAsync(ByteConverter.GetBytes(data), client).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(bool data, TcpClient? client = null) =>
        await TryWriteAsync(ByteConverter.GetBytes(data), client).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(short data, TcpClient? client = null) =>
        await TryWriteAsync(ByteConverter.GetBytes(data), client).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(long data, TcpClient? client = null) =>
        await TryWriteAsync(ByteConverter.GetBytes(data), client).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(byte data, TcpClient? client = null) =>
        await TryWriteAsync([data], client).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(char data, TcpClient? client = null) =>
        await TryWriteAsync(ByteConverter.GetBytes(data), client).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(decimal data, TcpClient? client = null) =>
        await TryWriteAsync(ByteConverter.GetBytes(data), client).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(string data, TcpClient? client = null) =>
        await TryWriteAsync(ByteConverter.GetBytes(data), client).ConfigureAwait(false);

    public async Task<bool> TrySendAsync(DateTime data, TcpClient? client = null) =>
        await TryWriteAsync(ByteConverter.GetBytes(data), client).ConfigureAwait(false);

    public async Task<bool> TrySendAsync<T>(T data, TcpClient? client = null) where T : AbstractMessage, new() =>
        await TryWriteAsync(data.Serialize(), client).ConfigureAwait(false);



    public async Task<bool> TryWriteAsync(byte[] bytes, TcpClient? client = null)
    {
        var header = new MessageHeader(length => (int)length + bytes.Length);

        header.ToBytes(out byte[] headerBytes);
        bytes = [.. headerBytes, .. bytes];

        try
        {
            var stream = _clients.GetStream(client);
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

    private void ReadLoop(TcpClient client, CancellationToken stoppingToken)
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

                var stopwatch = Stopwatch.StartNew();

                if (TryAssemble(buffer.AsMemory(0, bytesRead), out var integrity))
                {
                    OnMessage?.Invoke(Listener, client, integrity);
                }
            }
        }
        catch (OperationCanceledException e)
        {
            LogTcpListener(Logger, "the task has been canceled", e);
        }
        finally
        {
            client.Close();
        }


    }

    public async Task TryExecuteAsync()
    {
        try
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                var cancellationToken = CancellationToken.Token;
                var client = await Listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _clients.AddOrUpdate(client.Client.RemoteEndPoint!.ToString()!, client, (key, oldValue) => client);
                _ = Task.Run(() => ReadLoop(client, cancellationToken));
            }
        }
        catch (OperationCanceledException e)
        {
            LogTcpListener(Logger, "mission canceled", e);
            OnError?.Invoke(e);
        }
        finally
        {
            Listener.Stop();
        }
    }
    protected override void Dispose(bool disposing)
    {

        foreach (var client in _clients.Values)
        {
            if (disposing && client.Connected)
            {
                client.Dispose();
            }
        }
        base.Dispose(disposing);
    }
}