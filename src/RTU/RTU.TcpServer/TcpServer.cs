

using Microsoft.Extensions.Logging;
using RTU.Infrastructures.Contracts.Tcp;
using RTU.Infrastructures.Extensions.Tcp;
using RTU.TCPServer.Contracts;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace RTU.TCPServer;

internal sealed class TcpServer : Channel, ITcpServer
{
    private readonly CancellationTokenSource cancellationSource = new();
    internal TcpServer(ChannelOptions options, ILoggerFactory loggerFactory)
        : base(options, loggerFactory)
    {
    }
    public bool TryEnqueue(ReadOnlyMemory<byte> data)
    {
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

        return true;
    }

    private void ProcessClient(TcpClient client, CancellationToken stoppingToken)
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

                TryEnqueue(buffer.AsMemory(0, bytesRead));

                stopwatch.Stop();
                Console.WriteLine($"TryEnqueueAsyncºÄÊ±: {stopwatch.Elapsed.TotalMilliseconds} ºÁÃë");
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
            while (!cancellationSource.IsCancellationRequested)
            {
                var cancellationToken = cancellationSource.Token;
                var client = await Listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                if (client is not null)
                    ProcessClient(client, cancellationToken);
            }
        }
        catch (OperationCanceledException e)
        {
            LogTcpListener(Logger, "mission canceled", e);
        }
        finally
        {
            Listener.Stop();
        }
    }
    public override void Dispose()
    {
        cancellationSource.Cancel();
        cancellationSource.Dispose();
        base.Dispose();

    }
}