using Asprtu.Rtu.Attributes;
using Asprtu.Rtu.Contracts.DLT645;
using Asprtu.Rtu.DLT645.Contracts;
using Asprtu.Rtu.DLT645.Extensions;
using Asprtu.Rtu.DLT645.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

#if NET6_0_OR_GREATER
using RJCP.IO.Ports;

#else
using System.IO.Ports;
#endif
using ThrowHelper = Asprtu.Rtu.Extensions.ThrowHelper;

namespace Asprtu.Rtu.DLT645;

[LibraryCapacities]
public sealed class Dlt645Client : Channel, IDlt645Client
{
    private readonly ILogger<Dlt645Client> _logger;

    private readonly DataDecoder _decoder = new();

    public Action<Exception>? OnError { get; set; }

#if NET6_0_OR_GREATER
    public Action<SerialPortStream>? OnSuccess { get; set; }
    public Action<SerialPortStream, byte[]>? OnMessage { get; set; }


#else
    public Action<SerialPort>? OnSuccess { get; set; }
    public Action<SerialPort, byte[]>? OnMessage { get; set; }

#endif

    public Dlt645Client() : base(new("default"), NullLoggerFactory.Instance)
    {
        _logger = NullLogger<Dlt645Client>.Instance;
        Create();
    }
    public Dlt645Client(ChannelOptions options, ILoggerFactory loggerFactory) : base(options, loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Dlt645Client>();
        Create();
    }

    protected override void Create()
    {
        base.Create();

        foreach (var item in Ports)
        {
            if (item != null && item.IsOpen)
            {
                OnSuccess?.Invoke(item);
            }
        }
    }


    public async IAsyncEnumerable<SemanticValue> TrySendAsync(byte code, byte[] addresses, byte[]? data = null,
         [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        MessageHeader messageHeader = new(
           address: addresses,
           control: code,
           bytes: data
        );
        DataFormats.TryGet(code, out var def);

        var length = messageHeader.ToBytes(out var messageBytes);

        var messages = TryWriteAsync(messageBytes.AsMemory(0, length), cancellationToken);

        await foreach (var item in _decoder.TryDecodeAsync(messages, def!, OnError, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return item;
        }

    }

    public async IAsyncEnumerable<T> TrySendAsync<T>(byte code, byte[] addresses, byte[]? data = null,
         [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : SemanticValue
    {
        MessageHeader messageHeader = new(
            address: addresses,
            control: code,
            bytes: data
        );
        DataFormats.TryGet(code, out var def);

        ThrowHelper.ThrowIfNull(def);



        var length = messageHeader.ToBytes(out var messageBytes);

        var messages = TryWriteAsync(messageBytes.AsMemory(0, length), cancellationToken);

        await foreach (var item in _decoder.TryDecodeAsync<T>(messages, def!, OnError, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<T> TrySendAsync<T>(MessageHeader messageHeader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : SemanticValue
    {

        DataFormats.TryGet(messageHeader.Code, out var def);

        ThrowHelper.ThrowIfNull(def);

        var length = messageHeader.ToBytes(out var messageBytes);

        var messages = TryWriteAsync(messageBytes.AsMemory(0, length), cancellationToken);

        await foreach (var item in _decoder.TryDecodeAsync<T>(messages, def!, OnError, cancellationToken)
         .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<T> TrySendAsync<T>([NotNull] IEnumerable<MessageHeader> messages, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : SemanticValue
    {
        foreach (var message in messages)
        {
            await Task.Delay(35, cancellationToken)
                .ConfigureAwait(true); // 遵循 DL/T 645 协议的最小帧间隔时间 30ms，加一点余量

            DataFormats.TryGet(message.Code, out var def);

            ThrowHelper.ThrowIfNull(def);

            await foreach (var header in TrySendAsync<T>(message, cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
            {
                yield return header;
            }
        }
    }

    public IAsyncEnumerable<SemanticValue> TrySendAsync<T>(T command, byte[] addresses, byte[]? data = null, CancellationToken cancellationToken = default)
        where T : Enum
    {
        var type = typeof(T);
        if (Attribute.IsDefined(type, typeof(EnumCommandAttribute)))
            return TrySendAsync(Convert.ToByte(command), addresses, data, cancellationToken);
        return EmptyAsync<SemanticValue>();
    }
    public async IAsyncEnumerable<SemanticValue> TrySendAsync<T>(T command, string addresses, byte[]? data = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
         where T : Enum
    {
        var type = typeof(T);
        if (Attribute.IsDefined(type, typeof(EnumCommandAttribute)))
        {
            var commandByte = Convert.ToByte(command);
            foreach (var address in AddressFormatExtension.FormatAddresses(addresses))
            {
                await foreach (var item in TrySendAsync(commandByte, address, data, cancellationToken)
                    .ConfigureAwait(false))
                {
                    yield return item;
                }
            }
        }

        yield break;
    }
    public async IAsyncEnumerable<MessageHeader> TryWriteAsync(byte[] bytes,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var frame in TryWriteAsync(bytes.AsMemory(), cancellationToken)
            .ConfigureAwait(false))
        {
            yield return frame;
        }
    }

    public async IAsyncEnumerable<MessageHeader> TryWriteAsync(Memory<byte> buffer,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsPortsConnected())
        {
            OnError?.Invoke(new InvalidOperationException("No open serial ports available."));
            _logger.LogError("No open serial ports available.");
            yield return default;
        }

        // 广播到所有通道
        var sendTasks = Options.Channels.Distinct()
            .Select(com => WriteAsync(com.Port, buffer, cancellationToken));
        try
        {
            await Task.WhenAll(sendTasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Operation was canceled while sending broadcast frame");
            OnError?.Invoke(ex); // 触发 OnError
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error occurred while sending broadcast frame");
            OnError?.Invoke(ex); // 触发 OnError
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while sending broadcast frame");
            OnError?.Invoke(ex); // 触发 OnError
        }

        // 等待返回帧
        await foreach (var frame in ReceiveFrameAsync(cancellationToken)
            .WithCancellation(cancellationToken))
        {
            yield return frame;
        }
    }

    public async Task<IAsyncEnumerable<MessageHeader>> TryReadAddressAsync(CancellationToken cancellationToken = default)
    {
        if (!IsPortsConnected())
            throw new InvalidOperationException("No open serial ports available.");

        // 广播帧
        MessageHeader messageHeader = new(
           address: [.. Enumerable.Repeat((byte)0xAA, 6)],
           control: ((byte)Command.Code.ReadAddress)
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
                OnError?.Invoke(ex);
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
#pragma warning disable CA1031 // 不捕获常规异常类型
            try
            {
                await foreach (var header in seq.WithCancellation(cancellationToken))
                {
                    await outputChannel.Writer.WriteAsync(header, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                outputChannel.Writer.TryComplete(ex);
            }
#pragma warning restore CA1031 // 不捕获常规异常类型
        });

        // 启动一个任务来等待所有端口读取结束
        var completionTask = Task.WhenAll(portReadingTasks);

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


        try
        {
            await Task.WhenAll(portReadingTasks).ConfigureAwait(false);
            outputChannel.Writer.TryComplete(); // 正常完成
        }
        catch (Exception ex)
        {
            outputChannel.Writer.TryComplete(ex);
        }

    }

#if NET6_0_OR_GREATER
    private async IAsyncEnumerable<MessageHeader> ReadLoopAsync(
     SerialPortStream port,
     TimeSpan timeSpan,
     [EnumeratorCancellation] CancellationToken stoppingToken)
#else
    private async IAsyncEnumerable<MessageHeader> ReadLoopAsync(
      SerialPort port,
      TimeSpan timeSpan,
      [EnumeratorCancellation] CancellationToken stoppingToken)
#endif
    {
        ThrowHelper.ThrowIfNull(port);
        byte[] recvBuffer = new byte[1024];

        port.ReadTimeout = timeSpan.Milliseconds;
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1, stoppingToken)
                .ConfigureAwait(true); // 防止 tight loop
            int bytesRead;
            try
            {
                bytesRead = await ReadAsync(port.PortName, recvBuffer, 0, recvBuffer.Length, stoppingToken)
                    .ConfigureAwait(false); // 1024
            }
            catch (TimeoutException)
            {
                continue;
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (IOException ex)
            {
                OnError?.Invoke(ex);
                _logger.LogError(ex, "IO error occurred while reading from port {PortName}", port.PortName);
                yield break;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                _logger.LogError(ex, "Invalid operation while reading from port {PortName}", port.PortName);
                yield break;
            }


            if (bytesRead <= 0)
                continue;

            // 写入环形缓冲区
            Buffer.Write(recvBuffer.AsSpan(0, bytesRead));

            // 循环提取所有可用的完整帧（粘包处理）
            while (TryAssemble(out var frame))
            {
                OnMessage?.Invoke(port, frame);
                yield return new(frame);
            }
        }
    }

    private static async IAsyncEnumerable<T> EmptyAsync<T>()
    {
        yield break;
    }

    private bool TryAssemble(out byte[] frame)
    {
        frame = default!;

        try
        {
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

            if (len > 200) // 攻击/异常
            {
                Buffer.Read(1);  // 丢弃无效帧头
                OnError?.Invoke(new InvalidDataException($"DLT645 数据长度异常: {len}"));
                return false;
            }

            // 12固定头 + 数据区 + 校验 + 结束符
            int total = 12 + len;

            if (Buffer.Count < total)
                return false;

            frame = Buffer.Peek(total);

            // 验证结束符
            if (frame[^1] != 0x16)
            {
                frame = [];
                OnError?.Invoke(new InvalidDataException("DLT645 帧结束符无效。"));
                return false;
            }

            // 验证校验
            byte cs = frame[^2];
            byte sum = 0;
            for (int i = 0; i < frame.Length - 2; i++)
                sum += frame[i];

            if (sum != cs)
            {
                OnError?.Invoke(new InvalidDataException("DLT645 帧校验失败。"));
                return false;
            }
            frame = Buffer.Read(total);

            // 解析数据区
            if (frame.TryGetData(out byte[] data))
            {
                MessageHeaderExtensions.DecodeData(data);
            }

            return true;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            OnError?.Invoke(ex);
            return false;
        }
        catch (InvalidDataException ex)
        {
            OnError?.Invoke(ex);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 捕获其他未预期但允许的异常，排除取消操作异常
            OnError?.Invoke(ex);
            return false;
        }
    }

}