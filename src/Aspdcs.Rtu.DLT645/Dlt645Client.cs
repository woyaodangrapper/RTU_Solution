using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Aspdcs.Rtu.Contracts.DLT645;
using Aspdcs.Rtu.Attributes;

#if NET6_0_OR_GREATER

using RJCP.IO.Ports;

#else
using System.IO.Ports;
#endif

using ThrowHelper = Aspdcs.Rtu.Extensions.ThrowHelper;

namespace Aspdcs.Rtu.DLT645;

[LibraryCapacities]
public sealed class Dlt645Client : Channel, IDlt645Client
{
    private readonly ILogger<Dlt645Client> _logger;

    private readonly DataDecoder _decoder = new();

    // LoggerMessage 委托，用于高性能日志记录
    private static readonly Action<ILogger, Exception?> LogNoOpenSerialPorts =
        LoggerMessage.Define(LogLevel.Error, new EventId(1, nameof(LogNoOpenSerialPorts)),
            "No open serial ports available.");

    private static readonly Action<ILogger, Exception> LogOperationCanceled =
        LoggerMessage.Define(LogLevel.Error, new EventId(2, nameof(LogOperationCanceled)),
            "Operation was canceled while sending broadcast frame");

    private static readonly Action<ILogger, Exception> LogIOError =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(LogIOError)),
            "IO error occurred while sending broadcast frame");

    private static readonly Action<ILogger, Exception> LogInvalidOperation =
        LoggerMessage.Define(LogLevel.Error, new EventId(4, nameof(LogInvalidOperation)),
            "Invalid operation while sending broadcast frame");

    private static readonly Action<ILogger, int, int, Exception> LogBroadcastRetry =
        LoggerMessage.Define<int, int>(LogLevel.Error, new EventId(5, nameof(LogBroadcastRetry)),
            "Error sending broadcast frame, retrying {Retry}/{MaxRetries}");

    private static readonly Action<ILogger, string, Exception> LogIOErrorOnPort =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6, nameof(LogIOErrorOnPort)),
            "IO error occurred while reading from port {PortName}");

    private static readonly Action<ILogger, string, Exception> LogInvalidOperationOnPort =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(7, nameof(LogInvalidOperationOnPort)),
            "Invalid operation while reading from port {PortName}");

    public Action<Exception>? OnError { get; set; }

#if NET6_0_OR_GREATER
    public Action<SerialPortStream>? OnSuccess { get; set; }
    public Action<SerialPortStream, byte[]>? OnMessage { get; set; }

#else
    public Action<SerialPort>? OnSuccess { get; set; }
    public Action<SerialPort, byte[]>? OnMessage { get; set; }

#endif

    public Dlt645Client() : base(new("default"), NullLoggerFactory.Instance)
        => _logger = NullLogger<Dlt645Client>.Instance;

    public Dlt645Client(ChannelOptions options, ILoggerFactory? loggerFactory = null)
        : base(options, loggerFactory ?? NullLoggerFactory.Instance)
    {
        _logger = (loggerFactory ?? NullLoggerFactory.Instance)
            .CreateLogger<Dlt645Client>();
    }

    public override async Task RunAsync()
    {
        await base.RunAsync()
            .ConfigureAwait(false);

        foreach (var item in Ports)
        {
            if (item != null && item.IsOpen)
            {
                OnSuccess?.Invoke(item);
            }
        }
    }
    public override void Run()
    {
        base.Run();

        foreach (var item in Ports)
        {
            if (item != null && item.IsOpen)
            {
                OnSuccess?.Invoke(item);
            }
        }
    }

    internal async IAsyncEnumerable<SemanticValue> TrySendAsync(byte code, byte[] addresses, byte[]? data = null,
         [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        MessageHeader messageHeader = new(
           address: addresses,
           control: code,
           bytes: data
        );
        DataFormats.TryGet(data, out var def);

        ThrowHelper.ThrowIfNull(def);

        var length = messageHeader.ToBytes(out var messageBytes);

        var messages = TryWriteAsync(messageBytes.AsMemory(0, length), cancellationToken);

        await foreach (var item in _decoder.TryDecodeAsync(messages, def!, OnError, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    internal async IAsyncEnumerable<T> TrySendAsync<T>(byte code, byte[] addresses, byte[]? data = null,
         [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : SemanticValue
    {
        MessageHeader messageHeader = new(
            address: addresses,
            control: code,
            bytes: data
        );
        DataFormats.TryGet(data, out var def);

        ThrowHelper.ThrowIfNull(def);

        var length = messageHeader.ToBytes(out var messageBytes);

        var messages = TryWriteAsync(messageBytes.AsMemory(0, length), cancellationToken);

        await foreach (var item in _decoder.TryDecodeAsync<T>(messages, def!, OnError, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    internal async IAsyncEnumerable<T> TrySendAsync<T>(MessageHeader messageHeader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : SemanticValue
    {
        DataFormats.TryGet(messageHeader.Data, out var def);

        ThrowHelper.ThrowIfNull(def);

        var length = messageHeader.ToBytes(out var messageBytes);

        var messages = TryWriteAsync(messageBytes.AsMemory(0, length), cancellationToken);

        await foreach (var item in _decoder.TryDecodeAsync<T>(messages, def!, OnError, cancellationToken)
         .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    internal async IAsyncEnumerable<T> TrySendAsync<T>([NotNull] IEnumerable<MessageHeader> messages, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : SemanticValue
    {
        foreach (var message in messages)
        {
            await Task.Delay(35, cancellationToken)
                .ConfigureAwait(true); // 遵循 DL/T 645 协议的最小帧间隔时间 30ms，加一点余量

            DataFormats.TryGet(message.Data, out var def);

            ThrowHelper.ThrowIfNull(def);

            await foreach (var header in TrySendAsync<T>(message, cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
            {
                yield return header;
            }
        }
    }

    internal IAsyncEnumerable<SemanticValue> TrySendAsync<T>(T command, byte[] addresses, byte[]? data = null, CancellationToken cancellationToken = default)
        where T : Enum
    {
        var type = typeof(T);
        if (Attribute.IsDefined(type, typeof(EnumCommandAttribute)))
            return TrySendAsync(Convert.ToByte(command), addresses, data, cancellationToken);
        return EmptyAsync<SemanticValue>();
    }

    internal async IAsyncEnumerable<SemanticValue> TrySendAsync<T>(T command, string addresses, byte[]? data = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
            LogNoOpenSerialPorts(_logger, null);
            yield return default;
        }
        using var token = CancellationTokenExtensions
            .CreateTimeoutTokenIfNeeded(Options.Timeout, Options.RetryCount, cancellationToken, out var effectiveToken);

        // 广播到所有通道
        var sendTasks = Options.Channels.Distinct()
            .Select(com => WriteAsync(com.Port, buffer, cancellationToken));
        try
        {
            await Task.WhenAll(sendTasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            LogOperationCanceled(_logger, ex);
            OnError?.Invoke(ex); // 触发 OnError
        }
        catch (IOException ex)
        {
            LogIOError(_logger, ex);
            OnError?.Invoke(ex); // 触发 OnError
        }
        catch (InvalidOperationException ex)
        {
            LogInvalidOperation(_logger, ex);
            OnError?.Invoke(ex); // 触发 OnError
        }

        var expectedFrames = buffer.Span.IsBroadcast() ? -1 : Options.Channels.Count;
        // 等待返回帧

        await foreach (var header in token.Expect(expectedFrames, ReceiveFrameAsync(effectiveToken), Options.Timeout)
            .WithCancellation(cancellationToken))
        {
            yield return header;
        }

    }

    public async IAsyncEnumerable<SemanticValue> ReadAsync([NotNull] IEnumerable<AddressValue> addresses, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in ReadAsync(string.Join(";", addresses.Select(a => a.Address)), ct).ConfigureAwait(false))
        {
            yield return item;
        }
    }
    public async IAsyncEnumerable<SemanticValue> ReadAsync([NotNull] IEnumerable<AddressValue> addresses, uint dataId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in ReadAsync(addresses, dataId, ct).ConfigureAwait(false))
        {
            yield return item;
        }
    }


    public async IAsyncEnumerable<SemanticValue> ReadAsync(byte[] address, uint dataId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in TrySendAsync(Command.Code.ReadData, address, DataBuilder.Read(dataId), ct)
            .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<SemanticValue> ReadAsync(string address, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in TrySendAsync(Command.Code.ReadData, address, DataBuilder.Read((uint)Command.EnergyData.ForwardActiveTotalEnergy), ct)
            .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<SemanticValue> ReadAsync(string address, uint dataId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in TrySendAsync(Command.Code.ReadData, address, DataBuilder.Read(dataId), ct)
            .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<SemanticValue> ReadAsync(uint command, byte[] address, uint dataId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in TrySendAsync<SemanticValue>(Convert.ToByte(command), address, DataBuilder.Read(dataId), ct)
            .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<SemanticValue> ReadAsync(uint command, string addresses, uint dataId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var commandByte = Convert.ToByte(command);
        var dataBytes = DataBuilder.Read(dataId);
        foreach (var address in AddressFormatExtension.FormatAddresses(addresses))
        {
            await foreach (var item in TrySendAsync(commandByte, address, dataBytes, ct)
                .ConfigureAwait(false))
            {
                yield return item;
            }
        }
    }

    public async IAsyncEnumerable<SemanticValue> ReadNextAsync(byte[] address, byte frameIndex, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in TrySendAsync(Command.Code.ReadSubsequentData, address, DataBuilder.ReadNext(frameIndex), ct)
            .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<SemanticValue> ReadNextAsync(string addresses, byte frameIndex, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in TrySendAsync(Command.Code.ReadSubsequentData, addresses, DataBuilder.ReadNext(frameIndex), ct)
            .ConfigureAwait(false))
        {
            yield return item;
        }
    }


    public IAsyncEnumerable<SemanticValue> WriteAsync(byte[] address, uint dataId, uint password, uint operatorCode, ReadOnlySpan<byte> payload, CancellationToken ct = default)
        => TrySendAsync(Command.Code.WriteData, address, DataBuilder.Write(dataId, password, operatorCode, payload), ct);

    public IAsyncEnumerable<SemanticValue> WriteAsync(uint command, byte[] address, uint dataId, uint password, uint operatorCode, ReadOnlySpan<byte> payload, CancellationToken ct = default)
        => TrySendAsync(Convert.ToByte(command), address, DataBuilder.Write(dataId, password, operatorCode, payload), ct);

    public IAsyncEnumerable<SemanticValue> WriteAsync(string address, uint dataId, uint password, uint operatorCode, ReadOnlySpan<byte> payload, CancellationToken ct = default)
        => TrySendAsync(Command.Code.WriteData, address, DataBuilder.Write(dataId, password, operatorCode, payload), ct);

    public async IAsyncEnumerable<SemanticValue> WriteAsync(uint command, string addresses, uint dataId, uint password, uint operatorCode, byte[] payload,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var commandByte = Convert.ToByte(command);
        var dataBytes = DataBuilder.Write(dataId, password, operatorCode, payload);

        foreach (var address in AddressFormatExtension.FormatAddresses(addresses))
        {
            MessageHeader messageHeader = new(
                address: address,
                control: commandByte,
                bytes: dataBytes
            );

            await foreach (var item in TrySendAsync(commandByte, address, dataBytes, ct)
                .ConfigureAwait(false))
            {
                yield return item;
            }
        }
    }

    public async IAsyncEnumerable<AddressValue> TryReadAddressAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsPortsConnected())
            throw new InvalidOperationException("No open serial ports available.");

        using var token = CancellationTokenExtensions
           .CreateTimeoutTokenIfNeeded(Options.Timeout, Options.RetryCount, cancellationToken, out var effectiveToken);


        // 广播帧
        MessageHeader messageHeader = new(
           address: [.. Enumerable.Repeat((byte)0xAA, 6)],
           control: ((byte)Command.Code.ReadAddress)
        );

        int length = messageHeader.ToBytes(out var bytes);

        // 发送广播帧到所有打开的串口
        var sendTasks = Options.Channels.Distinct().Select(com => Task.Run(() =>
         Write(com.Port, bytes, 0, length), cancellationToken));
        await Task.WhenAll(sendTasks).ConfigureAwait(false);
        for (int retry = 0; retry <= Options.RetryCount; retry++)
        {
            try
            {
                await Task.WhenAll(sendTasks).ConfigureAwait(false);
                break;
            }
            catch (Exception ex) when (retry < Options.RetryCount)
            {
                LogBroadcastRetry(_logger, retry + 1, Options.RetryCount, ex);
                OnError?.Invoke(ex);
            }
        }

        // 等待返回帧 广播不会等待响应
        await foreach (var header in token.Expect(-1, ReceiveFrameAsync(effectiveToken), Options.Timeout)
            .WithCancellation(cancellationToken))
        {
#if NET6_0_OR_GREATER
            yield return new(Convert.ToHexString(header.Address));
#else
        string id = BitConverter.ToString(header.Address).Replace("-", "");
        yield return new(id);
#endif
        }

        yield break;
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

        DateTime lastResponseTime = DateTime.MinValue;


        bool canRead;
        try
        {
            canRead = await outputChannel.Reader
                .WaitToReadAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            yield break; // 所期望 Expect 正常完成
        }


        if (!canRead)
            yield break;


        while (outputChannel.Reader.TryRead(out var header))
        {
            if (continueCallback != null
                && continueCommandCode.HasValue
                && header.Code == continueCommandCode.Value)
            {
                await continueCallback(header).ConfigureAwait(false);
            }

            yield return header;
        }

#pragma warning disable CA1031 // 不捕获常规异常类型
        try
        {
            outputChannel.Writer.Complete(); // 正常完成
            await Task.WhenAll(portReadingTasks).ConfigureAwait(false);
        }

        catch (Exception ex)
        {
            outputChannel.Writer.TryComplete(ex);
        }
#pragma warning restore CA1031 // 不捕获常规异常类型
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


            // 为每次读取操作创建带超时的链接令牌，防止无限等待
            using var timeoutCts = new CancellationTokenSource(timeSpan);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

#pragma warning disable CA1031 // 不捕获常规异常类型
            try
            {

                bytesRead = await ReadAsync(port.PortName, recvBuffer, 0, recvBuffer.Length, linkedCts.Token)
                    .ConfigureAwait(false); // 1024
            }
            catch (TimeoutException)
            {
                continue;
            }
            catch (OperationCanceledException)
            {
                // 如果是超时令牌取消，继续下一次循环；如果是 stoppingToken 取消，退出
                if (stoppingToken.IsCancellationRequested)
                    yield break;
                continue;
            }
            catch (IOException ex)
            {
                OnError?.Invoke(ex);
                LogIOErrorOnPort(_logger, port.PortName, ex);
                yield break;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                LogInvalidOperationOnPort(_logger, port.PortName, ex);
                yield break;
            }
#pragma warning restore CA1031 // 不捕获常规异常类型

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
        yield break;
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
            if (frame.TryGetData(out var data))
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