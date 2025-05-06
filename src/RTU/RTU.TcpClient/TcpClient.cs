using Microsoft.Extensions.Logging;
using RTU.Infrastructures.Contracts.Tcp;
using RTU.Infrastructures.Extensions.Tcp;
using RTU.TcpClient.Contracts;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace RTU.TcpClient;
internal sealed class TcpClient : Channel, ITcpClient
{
    public System.Net.Sockets.TcpClient Client { get => Listener; }

    public Action<Exception>? OnError { get; set; }
    public Action<System.Net.Sockets.TcpClient>? OnSuccess { get; set; }
    public Action<System.Net.Sockets.TcpClient, byte[]>? OnMessage { get; set; }

    internal TcpClient(ChannelOptions options, ILoggerFactory loggerFactory)
        : base(options, loggerFactory)
    {
    }

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

    public async Task<bool> TrySendAsync<T>(T data) where T : AbstractMessage, new() =>
        await TryWriteAsync(data.Serialize()).ConfigureAwait(false);
    public async Task<bool> TryWriteAsync(byte[] bytes)
    {
        try
        {
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
        try
        {
            if (!Listener.Connected)
            {
                await Listener.ConnectAsync(IPAddress, Port).ConfigureAwait(false);
            }
            OnSuccess?.Invoke(Listener);

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
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}