using Aspdcs.Rtu.Attributes;
using Aspdcs.Rtu.Contracts.Tcp;
using Aspdcs.Rtu.Extensions.Tcp;
using Aspdcs.Rtu.TcpServer.Contracts;
using Aspdcs.Rtu.TcpServer.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Aspdcs.Rtu.TcpServer;

[LibraryCapacities]
public sealed class TcpServer : Channel, ITcpServer
{
    public TcpListener Server => Listener;

    public Action<Exception>? OnError { get; set; }
    public Action<TcpListener>? OnSuccess { get; set; }
    public Action<TcpListener, TcpClient, byte[]>? OnMessage { get; set; }

    private readonly ConcurrentDictionary<string, TcpClient> _clients = new();

    public TcpServer() : base(new("default"), NullLoggerFactory.Instance)
        => _tracker.SetState(ConnectionState.Listening);

    public TcpServer(ILoggerFactory loggerFactory) : base(new("default"), loggerFactory)
        => _tracker.SetState(ConnectionState.Listening);

    //[ActivatorUtilitiesConstructor]
    public TcpServer(ChannelOptions options, ILoggerFactory loggerFactory)
        : base(options, loggerFactory) => OnSuccess?.Invoke(Listener);

    private readonly ConnectionStateTracker _tracker = new();
    private TcpClient? _client;

    public TcpInfo TcpInfo => _tracker.GetSnapshot(
      _client?.Client.RemoteEndPoint as IPEndPoint,
      Listener.LocalEndpoint as IPEndPoint
    );

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

    public async Task<bool> TrySendAsync<T>([NotNull] T data, TcpClient? client = null) where T : AbstractMessage, new() =>
        await TryWriteAsync(data.Serialize(), client).ConfigureAwait(false);

    public async Task<bool> TryWriteAsync(byte[] bytes, TcpClient? client = null)
    {
        var header = new MessageHeader(length => length + bytes.Length);

        header.ToBytes(out byte[] headerBytes);
        bytes = [.. headerBytes, .. bytes];
        try
        {
            _tracker.AddSent(bytes.LongLength);

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

    private bool TryAssemble(out byte[] integrity)
    {
        integrity = null!;
        int headerSize = Marshal.SizeOf<MessageHeader>();

        // 1. 缓冲区数据不足以读出头部
        if (Buffer.Count < headerSize)
            return false;

        // 2. 取头部字节但不移除
        byte[] headerBytes = Buffer.Peek(headerSize);
        var header = headerBytes.TryReadHeader();
        if (!header.HasValue)
            return false;

        long totalLength = header.Value.TotalLength;

        // 3. 缓冲区数据不足以读出整条消息
        if (Buffer.Count < totalLength)
            return false;

        // 4. 读出完整消息（含头、体、padding），并从缓冲区移除
        integrity = Buffer.Read((int)totalLength);
        return true;
    }

    private async Task ReadLoopAsync(TcpClient client, CancellationToken stoppingToken)
    {
        using var stream = client.GetStream();
        var recvBuffer = new byte[1024];

        while (!stoppingToken.IsCancellationRequested)
        {
            // 1. 异步读取网络流
            int bytesRead = await stream
                .ReadAsync(recvBuffer, stoppingToken)
                .ConfigureAwait(false);
            if (bytesRead == 0)
                break; // 客户端断开

            // 2. 写入环形缓冲区
            Buffer.Write(recvBuffer.AsSpan(0, bytesRead));

            // 3. 循环拆包，直到没有完整消息为止
            while (TryAssemble(out var message))
            {
                // 触发事件
                OnMessage?.Invoke(Server, client, message);
            }
        }
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
                if (TryAssemble(buffer.AsMemory(0, bytesRead), out var integrity))
                {
                    _client = client;
                    _tracker.AddReceived(integrity.LongLength);
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
        _tracker.SetState(ConnectionState.Active);

        try
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                var cancellationToken = CancellationToken.Token;

                TcpClient client;
#if NET5_0_OR_GREATER
                client = await Listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
#else
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var acceptTask = Listener.AcceptTcpClientAsync();
                var completedTask = await Task.WhenAny(acceptTask, Task.Delay(Timeout.Infinite, cts.Token));
                if (completedTask == acceptTask)
                {
                    cts.Cancel();
                    client = await acceptTask;
                }
                else
                {
                    throw new OperationCanceledException(cancellationToken);
                }
#endif

                _clients.AddOrUpdate(client.Client.RemoteEndPoint!.ToString()!, client, (key, oldValue) => client);
                _ = Task.Run(async () => await ReadLoopAsync(client, cancellationToken).ConfigureAwait(false));
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
        _tracker.SetState(ConnectionState.Closed);
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
        _tracker.SetState(ConnectionState.Closing);
        base.Dispose(disposing);
    }
}