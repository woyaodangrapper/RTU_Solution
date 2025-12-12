using Asprtu.Rtu.Attributes;
using Asprtu.Rtu.Contracts.DLT645;
using Asprtu.Rtu.DLT645.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RJCP.IO.Ports;
using System.Runtime.CompilerServices;

namespace Asprtu.Rtu.DLT645;

[LibraryCapacities]
public sealed class Dlt645Client : Channel, IDlt645Client
{
    private readonly ILogger<Dlt645Client> _logger;


    public Action<Exception>? OnError { get; set; }
    public Action<SerialPortStream>? OnSuccess { get; set; }
    public Action<SerialPortStream, byte[]>? OnMessage { get; set; }
    public Dlt645Client() : base(new("default"), NullLoggerFactory.Instance)
        => _logger = NullLogger<Dlt645Client>.Instance;
    public Dlt645Client(ChannelOptions options, ILoggerFactory loggerFactory) : base(options, loggerFactory)
        => _logger = loggerFactory.CreateLogger<Dlt645Client>();



    public Task<bool> TrySendAsync(byte data, out byte[] message)
    {
        throw new NotImplementedException();
    }


    public Task<bool> TrySendAsync<T>(T data)
        where T : AbstractMessage, new()
    {
        throw new NotImplementedException();
    }

    public Task<bool> TryWriteAsync(byte[] bytes)
    {
        throw new NotImplementedException();
    }



    public async Task<IAsyncEnumerable<MessageHeader>> TryReadAddressAsync(CancellationToken cancellationToken = default)
    {
        if (!IsPortsConnected())
        {
            throw new InvalidOperationException("No open serial ports available.");
        }

        // 广播帧
        MessageHeader messageHeader = new(
           address: [.. Enumerable.Repeat((byte)0xAA, 6)],
           control: ((byte)Command.ControlCode.ReadAddress),
           bytes: []
        );

        int length = messageHeader.ToBytes(out var bytes);

        // 发送广播帧到所有打开的串口
        var sendTasks = Options.Channels.Distinct().Select(com => Task.Run(() =>
         Write(com.Port, bytes, 0, length), cancellationToken));

        for (int retry = 0; retry <= Options.RetryCount; retry++)
        {
            try
            {
                await Task.WhenAll(sendTasks).ConfigureAwait(false);
                break;
            }
            catch (Exception ex) when (retry < Options.RetryCount)
            {
                _logger.LogError(ex, "Error sending broadcast frame, retrying {Retry}/{MaxRetries}", retry + 1, Options.RetryCount);
            }
        }

        // 广播不会等待响应
        return ReceiveFrameAsync(cancellationToken);
    }


    private async IAsyncEnumerable<MessageHeader> ReceiveFrameAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken,
        byte? continueCommandCode = null, Func<MessageHeader, Task>? continueCallback = null)
    {
        var readTasks = new List<IAsyncEnumerable<MessageHeader>>();

        foreach (var port in Ports.Where(p => p.IsOpen))
        {
            // 针对每个端口启动一个持续读取的序列
            readTasks.Add(ReadLoopAsync(port, Options.Timeout, cancellationToken));
        }

        var outputChannel = System.Threading.Channels.Channel.CreateUnbounded<MessageHeader>();

        var portReadingTasks = readTasks.Select(async seq =>
        {
            await foreach (var header in seq.WithCancellation(cancellationToken))
            {
                await outputChannel.Writer.WriteAsync(header, cancellationToken).ConfigureAwait(false);
            }
        });

        // 启动一个任务来等待所有端口读取结束
        var completionTask = Task.WhenAll(portReadingTasks);

        try
        {
            while (await outputChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (outputChannel.Reader.TryRead(out var header))
                {
                    if (continueCallback != null
                        && continueCommandCode.HasValue
                        && header.Code == continueCommandCode.Value)
                    {
                        await continueCallback(header).ConfigureAwait(false);
                    }
                    yield return header;
                }
            }
        }
        finally
        {
            outputChannel.Writer.Complete();
            await Task.WhenAny(completionTask)
                .ConfigureAwait(false);
        }
    }


    private async IAsyncEnumerable<MessageHeader> ReadLoopAsync(
     SerialPortStream port,
     TimeSpan timeSpan,
     [EnumeratorCancellation] CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(port);
        byte[] recvBuffer = new byte[1024];

        port.ReadTimeout = timeSpan.Milliseconds;
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1, stoppingToken)
                .ConfigureAwait(true); // 防止 tight loop
            int bytesRead;
            try
            {
                bytesRead = Read(
                    port.PortName,      // 入参要求
                    recvBuffer,         // 缓冲
                    0,
                    recvBuffer.Length); // 1024
            }
            catch (TimeoutException)
            {
                continue;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error occurred while reading from port {PortName}", port.PortName);
                break;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while reading from port {PortName}", port.PortName);
                break;
            }

            if (bytesRead <= 0)
                continue;

            // 写入环形缓冲区
            Buffer.Write(recvBuffer.AsSpan(0, bytesRead));


            if (!TryAssemble(out var frame))
                continue;

            yield return new(frame);
        }
    }


    private bool TryAssemble(out byte[] frame)
    {
        frame = default!;

        // 跳过前导 FE
        while (Buffer.Count > 0 && Buffer.Peek(1)[0] == 0xFE)
            Buffer.Read(1);

        // 不足以读取固定头部
        if (Buffer.Count < 10)
            return false;

        // 固定头部：68 + Address(6) + 68 + C + L = 10 字节
        var header = Buffer.Peek(10);

        if (header[0] != 0x68 || header[7] != 0x68)
        {
            Buffer.Read(1);
            return false;
        }

        // L = 数据区长度
        byte len = header[9];

        // 12固定头 + 数据区 + 校验 + 结束符
        int total = 12 + len;

        if (Buffer.Count < total)
            return false;

        frame = Buffer.Read(total);

        // 验证结束符
        if (frame[^1] != 0x16)
            return false;

        // 验证校验
        byte cs = frame[^2];
        byte sum = 0;
        for (int i = 0; i < frame.Length - 2; i++)
            sum += frame[i];

        if (sum != cs)
            return false;

        return true;
    }

}