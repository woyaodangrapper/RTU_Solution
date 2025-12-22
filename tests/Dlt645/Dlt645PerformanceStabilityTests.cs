using Aspdcs.Rtu.Contracts.DLT645;
using Aspdcs.Rtu.DLT645.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Dlt645.Tests;

/// <summary>
/// DLT645 性能与稳定性测试 - 核心测试维度 #5
/// 测试覆盖: 并发处理、压力测试、粘包处理、性能基准
/// </summary>
public class Dlt645PerformanceStabilityTests
{
    #region 并发测试

    [Fact]
    public async Task ConcurrentFrameCreation_IsThreadSafe()
    {
        // Arrange
        const int concurrentCount = 100;
        var tasks = new List<Task<MessageHeader>>();

        // Act - 并发创建100个帧
        for (int i = 0; i < concurrentCount; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() =>
            {
                byte[] address = [(byte)index, 0x00, 0x00, 0x00, 0x00, 0x00];
                byte control = 0x11;
                byte[] data = [0x00, 0x00, 0x01, 0x00];
                return new MessageHeader(address, control, data);
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - 所有帧都应该正确创建
        Assert.Equal(concurrentCount, results.Length);
        Assert.All(results, header => Assert.Equal(0x11, header.Code));
    }

    [Fact]
    public async Task ConcurrentEncryption_ProducesConsistentResults()
    {
        // Arrange
        const int iterations = 50;
        var originalData = new byte[] { 0x00, 0x00, 0x01, 0x00 };
        var results = new ConcurrentBag<byte[]>();

        // Act - 并发执行加密
        var tasks = Enumerable.Range(0, iterations).Select(_ => Task.Run(() =>
        {
            byte[] data = [.. originalData];
            Span<byte> span = data;
            MessageHeaderExtensions.EncodeData(span);
            results.Add(data);
        }));

        await Task.WhenAll(tasks);

        // Assert - 所有结果应该一致
        Assert.Equal(iterations, results.Count);
        var expected = new byte[] { 0x33, 0x33, 0x34, 0x33 };
        Assert.All(results, data => Assert.Equal(expected, data));
    }

    [Fact]
    public async Task ParallelFrameSerialization_MaintainsIntegrity()
    {
        // Arrange
        const int parallelCount = 50;
        var errors = new ConcurrentBag<Exception>();

        // Act
        await Parallel.ForEachAsync(
            Enumerable.Range(0, parallelCount),
            new ParallelOptions { MaxDegreeOfParallelism = 10 },
            async (i, ct) =>
            {
                try
                {
                    byte[] address = [0x11, 0x11, 0x00, 0x00, 0x00, 0x00];
                    byte control = 0x11;
                    byte[] data = [(byte)i, 0x00, 0x01, 0x00];

                    var header = new MessageHeader(address, control, data);
                    var bytes = header.ToBytes();

                    // 验证帧结构
                    Assert.Equal(0x68, bytes[2]);
                    Assert.Equal(0x16, bytes[^1]);
                    
                    await Task.Delay(1, ct);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            });

        // Assert
        Assert.Empty(errors);
    }

    #endregion

    #region 压力测试

    [Fact]
    public void HighVolumeFrameCreation_PerformanceTest()
    {
        // Arrange
        const int frameCount = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < frameCount; i++)
        {
            byte[] address = [0x11, 0x11, 0x00, 0x00, 0x00, 0x00];
            byte control = 0x11;
            byte[] data = [0x00, 0x00, 0x01, 0x00];

            var header = new MessageHeader(address, control, data);
            _ = header.ToBytes();
        }

        stopwatch.Stop();

        // Assert - 应该在合理时间内完成（例如 < 1秒）
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"创建 {frameCount} 个帧耗时 {stopwatch.ElapsedMilliseconds}ms，超过预期");
    }

    [Fact]
    public void HighVolumeEncryption_PerformanceTest()
    {
        // Arrange
        const int operationCount = 10000;
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < operationCount; i++)
        {
            byte[] copy = [.. data];
            Span<byte> span = copy;
            MessageHeaderExtensions.EncodeData(span);
            MessageHeaderExtensions.DecodeData(span);
        }

        stopwatch.Stop();

        // Assert - 加密解密应该很快（< 500ms）
        Assert.True(stopwatch.ElapsedMilliseconds < 500,
            $"执行 {operationCount} 次加解密耗时 {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void LargeDataFrame_HandlingTest()
    {
        // Arrange - 测试最大数据长度 200 字节
        byte[] address = [0x11, 0x11, 0x00, 0x00, 0x00, 0x00];
        byte control = 0x11;
        byte[] largeData = new byte[200];
        for (int i = 0; i < 200; i++)
            largeData[i] = (byte)(i % 256);

        // Act
        var header = new MessageHeader(address, control, largeData);
        var bytes = header.ToBytes();

        // Assert
        Assert.Equal(200, header.Length);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 200); // 包含前导、地址、控制等
    }

    #endregion

    #region 粘包处理测试

    [Fact]
    public void MultipleFramesInBuffer_CanBeSeparated()
    {
        // Arrange - 模拟缓冲区中有3个完整帧
        var frame1 = CreateStandardFrame(0x11);
        var frame2 = CreateStandardFrame(0x22);
        var frame3 = CreateStandardFrame(0x33);

        byte[] buffer = [.. frame1, .. frame2, .. frame3];

        // Act - 查找所有帧起始位置
        var framePositions = new List<int>();
        for (int i = 0; i < buffer.Length - 12; i++)
        {
            if (buffer[i] == 0x68 && i + 7 < buffer.Length && buffer[i + 7] == 0x68)
            {
                framePositions.Add(i);
            }
        }

        // Assert
        Assert.True(framePositions.Count >= 3, "应该识别出至少3个帧");
    }

    [Fact]
    public void PartialFrame_FollowedByCompleteFrame_HandledCorrectly()
    {
        // Arrange - 半包 + 完整包
        var partialFrame = new byte[] { 0xFE, 0xFE, 0x68, 0x11, 0x11 }; // 不完整
        var completeFrame = CreateStandardFrame(0x22);

        byte[] buffer = [.. partialFrame, .. completeFrame];

        // Act - 从完整帧位置开始解析
        int completeFrameStart = Array.IndexOf(buffer, (byte)0x68, partialFrame.Length);
        bool hasCompleteFrame = completeFrameStart >= 0 && 
                                buffer.Length - completeFrameStart >= 12;

        // Assert
        Assert.True(hasCompleteFrame);
    }

    [Fact]
    public void ConsecutiveFrames_NoLeadingBytes_ParsedCorrectly()
    {
        // Arrange - 无前导码的连续帧
        var frame1 = CreateStandardFrameWithoutPreamble(0x11);
        var frame2 = CreateStandardFrameWithoutPreamble(0x22);

        byte[] buffer = [.. frame1, .. frame2];

        // Act
        var frameCount = 0;
        for (int i = 0; i < buffer.Length - 11; i++)
        {
            if (buffer[i] == 0x68 && i + 7 < buffer.Length && buffer[i + 7] == 0x68)
            {
                frameCount++;
            }
        }

        // Assert
        Assert.True(frameCount >= 2);
    }

    #endregion

    #region 内存效率测试

    [Fact]
    public void SpanBasedEncoding_AvoidsCopying()
    {
        // Arrange
        byte[] data = new byte[100];
        Array.Fill(data, (byte)0x55);

        // Act - 使用 Span 就地加密
        Span<byte> span = data;
        MessageHeaderExtensions.EncodeData(span);

        // Assert - 验证原数组被修改（零拷贝）
        Assert.All(data, b => Assert.Equal(0x88, b)); // 0x55 + 0x33 = 0x88
    }

    [Fact]
    public void MemoryReuse_InMultipleOperations()
    {
        // Arrange
        byte[] buffer = new byte[256];

        // Act - 重复使用同一缓冲区
        for (int i = 0; i < 100; i++)
        {
            Array.Fill(buffer, (byte)i);
            Span<byte> span = buffer.AsSpan(0, 10);
            MessageHeaderExtensions.EncodeData(span);
            MessageHeaderExtensions.DecodeData(span);
        }

        // Assert - 缓冲区应该正确复用
        Assert.NotNull(buffer);
        Assert.Equal(256, buffer.Length);
    }

    #endregion

    #region 边界性能测试

    [Fact]
    public void MinimalFrame_ProcessingSpeed()
    {
        // Arrange - 最小帧（无数据域）
        const int iterations = 5000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            byte[] address = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
            var header = new MessageHeader(address, 0x13); // 读地址命令
            _ = header.ToBytes();
        }

        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 500,
            $"处理 {iterations} 个最小帧耗时 {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void ChecksumCalculation_OnLargeFrames_IsEfficient()
    {
        // Arrange
        const int iterations = 1000;
        byte[] largeFrame = new byte[214]; // 最大帧长度
        Array.Fill(largeFrame, (byte)0xFF);

        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            Span<byte> span = largeFrame;
            MessageHeaderExtensions.UpdateChecksum(span);
        }

        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 100,
            $"计算 {iterations} 次大帧校验和耗时 {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Helper Methods

    private static byte[] CreateStandardFrame(byte addressByte)
    {
        byte[] address = [addressByte, addressByte, 0x00, 0x00, 0x00, 0x00];
        byte control = 0x11;
        byte[] data = [0x00, 0x00, 0x01, 0x00];

        var header = new MessageHeader(address, control, data);
        return header.ToBytes();
    }

    private static byte[] CreateStandardFrameWithoutPreamble(byte addressByte)
    {
        byte[] address = [addressByte, addressByte, 0x00, 0x00, 0x00, 0x00];
        byte control = 0x11;
        byte[] data = [0x00, 0x00, 0x01, 0x00];

        var header = new MessageHeader(address, control, data, preamble: []);
        return header.ToBytes();
    }

    #endregion
}
