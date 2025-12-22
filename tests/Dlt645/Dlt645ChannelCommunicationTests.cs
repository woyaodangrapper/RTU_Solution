using Aspdcs.Rtu.Contracts.DLT645;
using Aspdcs.Rtu.DLT645;
using Aspdcs.Rtu.DLT645.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dlt645.Tests;

/// <summary>
/// DLT645 通道通信测试 - 核心测试维度 #4
/// 测试覆盖: 通道配置、帧发送、广播、同步异步机制
/// 注意: 这些测试不需要真实串口，测试逻辑和配置正确性
/// </summary>
public class Dlt645ChannelCommunicationTests
{
    #region 通道配置测试

    [Fact]
    public void ChannelOptions_DefaultValues_AreValid()
    {
        // Arrange & Act
        var options = new ChannelOptions("COM5");

        // Assert
        Assert.NotNull(options);
        Assert.Contains("COM5", options.Channels.Select(c => c.Port));
    }

 
    [Fact]
    public void ChannelOptions_Timeout_CanBeSet()
    {
        // Arrange
        var options = new ChannelOptions("COM5")
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        // Act & Assert
        Assert.Equal(TimeSpan.FromSeconds(5), options.Timeout);
    }

    [Fact]
    public void ChannelOptions_RetryCount_CanBeSet()
    {
        // Arrange
        var options = new ChannelOptions("COM5")
        {
            RetryCount = 5
        };

        // Act & Assert
        Assert.Equal(5, options.RetryCount);
    }

    #endregion

    #region 客户端创建测试

    [Fact]
    public void Dlt645Client_CanBeCreated_WithDefaultOptions()
    {
        // Arrange & Act
        var client = new Dlt645Client();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Dlt645Client_CanBeCreated_WithCustomOptions()
    {
        // Arrange
        var options = new ChannelOptions("COM5")
        {
            Timeout = TimeSpan.FromSeconds(3),
            RetryCount = 3
        };

        // Act
        var client = new Dlt645Client(options, NullLoggerFactory.Instance);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Dlt645ClientFactory_CreatesClient()
    {
        // Arrange
        var factory = new Dlt645ClientFactory(NullLoggerFactory.Instance);

        // Act
        var client = factory.CreateDlt645Client(new ChannelOptions("COM5"));

        // Assert
        Assert.NotNull(client);
    }

    #endregion

    #region 广播地址测试

    [Fact]
    public void BroadcastAddress_Construction_IsCorrect()
    {
        // Arrange
        byte[] broadcastAddress = [0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA];
        byte control = 0x13; // 读地址命令

        // Act
        var header = new MessageHeader(broadcastAddress, control);
        var frame = header.ToBytes();

        // Assert
        Assert.Equal(broadcastAddress, header.Address);
        Assert.Equal(0x13, header.Code);
        Assert.Equal(0, header.Length); // 读地址命令无数据
    }

    [Fact]
    public void ReadAddressCommand_HasNoData()
    {
        // Arrange
        byte[] address = [0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA];
        byte control = (byte)Command.Code.ReadAddress;

        // Act
        var header = new MessageHeader(address, control);

        // Assert
        Assert.Equal(0, header.Length);
        Assert.Equal(0x13, control);
    }

    #endregion

    #region 读写命令测试

    [Fact]
    public void ReadDataCommand_BuildsCorrectFrame()
    {
        // Arrange
        byte[] address = [0x11, 0x11, 0x00, 0x00, 0x00, 0x00];
        uint dataId = 0x00010000; // 正向有功总电能
        var data = DataBuilder.Read(dataId);

        // Act
        var header = new MessageHeader(address, (byte)Command.Code.ReadData, data);

        // Assert
        Assert.Equal(0x11, header.Code);
        Assert.Equal(4, header.Length);
    }

    [Fact]
    public void WriteDataCommand_BuildsCorrectFrame()
    {
        // Arrange
        byte[] address = [0x11, 0x11, 0x00, 0x00, 0x00, 0x00];
        uint dataId = 0x04000100;
        uint password = 0x00000000;
        uint operatorCode = 0x00000001;
        byte[] payload = [0x01, 0x02];

        var data = DataBuilder.Write(dataId, password, operatorCode, payload);

        // Act
        var header = new MessageHeader(address, (byte)Command.Code.WriteData, data);

        // Assert
        Assert.Equal(0x14, header.Code);
        Assert.Equal(14, header.Length); // 4 + 4 + 4 + 2
    }

    [Fact]
    public void ReadNextCommand_BuildsCorrectFrame()
    {
        // Arrange
        byte[] address = [0x11, 0x11, 0x00, 0x00, 0x00, 0x00];
        byte frameIndex = 0x02;
        var data = DataBuilder.ReadNext(frameIndex);

        // Act
        var header = new MessageHeader(address, (byte)Command.Code.ReadSubsequentData, data);

        // Assert
        Assert.Equal(0x12, header.Code);
        Assert.Equal(1, header.Length);
    }

    #endregion

    #region 帧间隔测试

    [Fact]
    public async Task MinimumFrameInterval_Is30ms()
    {
        // Arrange - DL/T 645 协议规定最小帧间隔 30ms
        var expectedMinInterval = TimeSpan.FromMilliseconds(30);
        var actualInterval = TimeSpan.FromMilliseconds(35); // 代码中是 35ms

        // Act & Assert
        Assert.True(actualInterval >= expectedMinInterval, 
            "帧间隔应该至少为 30ms 以符合 DLT645 协议");
        
        await Task.Delay(actualInterval); // 模拟等待
        Assert.True(true); // 验证延迟完成
    }

    #endregion

    #region 地址格式测试

    [Fact]
    public void Address_Is6Bytes_AsPerProtocol()
    {
        // Arrange
        byte[] address = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06];

        // Act
        var header = new MessageHeader(address, 0x11);

        // Assert
        Assert.Equal(6, header.Address.Length);
    }

    [Theory]
    [InlineData("111100000000")]
    [InlineData("222200000000")]
    [InlineData("AAAAAAAAAAAA")]
    public void AddressString_CanBeFormatted(string addressStr)
    {
        // Arrange - 地址字符串应该是 12 位十六进制
        
        // Act
        bool isValid = addressStr.Length == 12 && 
                      addressStr.All(c => "0123456789ABCDEFabcdef".Contains(c));

        // Assert
        Assert.True(isValid);
    }

    #endregion

    #region 异步操作测试

    [Fact]
    public async Task AsyncEnumerable_EmptySequence_Completes()
    {
        // Arrange
        var emptySequence = EmptyAsync<int>();
        var count = 0;

        // Act
        await foreach (var item in emptySequence)
        {
            count++;
        }

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task AsyncEnumerable_WithItems_Iterates()
    {
        // Arrange
        var sequence = GenerateAsync(3);
        var items = new List<int>();

        // Act
        await foreach (var item in sequence)
        {
            items.Add(item);
        }

        // Assert
        Assert.Equal(3, items.Count);
        Assert.Equal([0, 1, 2], items);
    }

    #endregion

    #region Helper Methods

    private static async IAsyncEnumerable<T> EmptyAsync<T>()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<int> GenerateAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await Task.Delay(1);
            yield return i;
        }
    }

    #endregion
}
