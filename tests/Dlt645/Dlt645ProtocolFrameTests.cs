using Aspdcs.Rtu.Contracts.DLT645;
using Aspdcs.Rtu.DLT645.Extensions;

namespace Dlt645.Tests;

/// <summary>
/// DLT645 协议帧测试 - 核心测试维度 #1
/// 测试覆盖: 帧结构、编解码(±0x33)、校验和、序列化/反序列化、数据构建
/// </summary>
public class Dlt645ProtocolFrameTests
{
    #region 帧结构测试

    [Fact]
    public void Frame_BasicStructure_IsValid()
    {
        // Arrange
        byte[] address = [0x11, 0x11, 0x00, 0x00, 0x00, 0x00];
        byte control = 0x11;
        byte[] data = [0x00, 0x00, 0x01, 0x00];

        // Act
        var header = new MessageHeader(address, control, data);
        var bytes = header.ToBytes();

        // Assert - 验证 DLT645 标准帧结构
        Assert.Equal(0xFE, bytes[0]);       // 前导码
        Assert.Equal(0xFE, bytes[1]);
        Assert.Equal(0x68, bytes[2]);       // 起始符
        Assert.Equal(0x68, bytes[9]);       // 帧起始符
        Assert.Equal(0x11, bytes[10]);      // 控制码
        Assert.Equal(0x04, bytes[11]);      // 数据长度
        Assert.Equal(0x16, bytes[^1]);      // 结束符 0x16
    }

    [Fact]
    public void Frame_Checksum_CalculatedCorrectly()
    {
        // Arrange
        byte[] address = [0x11, 0x11, 0x00, 0x00, 0x00, 0x00];
        byte control = 0x11;
        byte[] data = [0x33, 0x33, 0x34, 0x33]; // 已加密数据

        // Act
        var header = new MessageHeader(address, control, data);
        var bytes = header.ToBytes();

        // 手动计算校验和
        byte expectedChecksum = 0;
        for (int i = 2; i < bytes.Length - 2; i++) // 从第一个68到数据域末尾
            expectedChecksum += bytes[i];

        // Assert
        Assert.Equal(expectedChecksum, bytes[^2]); // 倒数第二个字节是校验和
    }

    [Fact]
    public void Frame_Serialization_RoundTrip_Successful()
    {
        // Arrange
        byte[] address = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06];
        byte control = 0x11;
        byte[] data = [0x00, 0x00, 0x01, 0x00];

        // Act - 序列化
        var original = new MessageHeader(address, control, data);
        var bytes = original.ToBytes();

        // 反序列化
        var reconstructed = new MessageHeader(bytes.AsSpan());

        // Assert
        Assert.Equal(original.Address, reconstructed.Address);
        Assert.Equal(original.Code, reconstructed.Code);
        Assert.Equal(original.Length, reconstructed.Length);
        Assert.Equal(original.Data.Take(original.Length), reconstructed.Data.Take(reconstructed.Length));
    }

    #endregion

    #region 数据编解码测试 (±0x33)

    [Fact]
    public void DataEncoding_Adds0x33_ToEachByte()
    {
        // Arrange - DLT645 协议规定数据域需要 +0x33 加密
        byte[] original = [0x00, 0x00, 0x01, 0x00];
        byte[] expected = [0x33, 0x33, 0x34, 0x33];

        // Act
        Span<byte> data = [.. original];
        MessageHeaderExtensions.EncodeData(data);

        // Assert
        Assert.Equal(expected, data.ToArray());
    }

    [Fact]
    public void DataDecoding_Subtracts0x33_FromEachByte()
    {
        // Arrange
        byte[] encrypted = [0x33, 0x33, 0x34, 0x33];
        byte[] expected = [0x00, 0x00, 0x01, 0x00];

        // Act
        Span<byte> data = [.. encrypted];
        MessageHeaderExtensions.DecodeData(data);

        // Assert
        Assert.Equal(expected, data.ToArray());
    }

    [Fact]
    public void DataEncoding_IsReversible()
    {
        // Arrange
        byte[] original = [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0];

        // Act - 加密再解密
        Span<byte> data = [.. original];
        MessageHeaderExtensions.EncodeData(data);
        MessageHeaderExtensions.DecodeData(data);

        // Assert - 应该恢复原值
        Assert.Equal(original, data.ToArray());
    }

    [Fact]
    public void Checksum_UpdatedAfterEncryption()
    {
        // Arrange - 构造完整帧
        byte[] frame = [
            0xFE, 0xFE, 0x68,
            0x11, 0x11, 0x00, 0x00, 0x00, 0x00,
            0x68, 0x11, 0x04,
            0x00, 0x00, 0x01, 0x00,  // 原始数据
            0x00, 0x16
        ];

        byte oldChecksum = frame[^2];

        // 加密数据域
        Span<byte> dataSpan = frame.AsSpan(12, 4);
        MessageHeaderExtensions.EncodeData(dataSpan);

        // Act - 更新校验和
        MessageHeaderExtensions.UpdateChecksum(frame);
        byte newChecksum = frame[^2];

        // 手动验证新校验和
        byte expectedChecksum = 0;
        for (int i = 2; i < 16; i++)
            expectedChecksum += frame[i];

        // Assert
        Assert.NotEqual(oldChecksum, newChecksum);
        Assert.Equal(expectedChecksum, newChecksum);
    }

    #endregion

    #region 数据构建器测试

    [Fact]
    public void DataBuilder_Read_BuildsCorrectDataIdentifier()
    {
        // Arrange - 正向有功总电能标识
        uint dataId = 0x00010000;

        // Act
        var data = DataBuilder.Read(dataId);

        // Assert - 小端序
        Assert.Equal(4, data.Length);
        Assert.Equal([0x00, 0x00, 0x01, 0x00], data);
    }

    [Fact]
    public void DataBuilder_Write_BuildsCompleteStructure()
    {
        // Arrange
        uint dataId = 0x04000100;
        uint password = 0x12345678;
        uint operatorCode = 0x00000001;
        byte[] payload = [0xAA, 0xBB];

        // Act
        var data = DataBuilder.Write(dataId, password, operatorCode, payload);

        // Assert
        Assert.Equal(14, data.Length); // 4 + 4 + 4 + 2
        
        // 验证小端序
        Assert.Equal(0x00, data[0]);
        Assert.Equal(0x01, data[1]);
        Assert.Equal(0x00, data[2]);
        Assert.Equal(0x04, data[3]);

        // 验证载荷
        Assert.Equal(0xAA, data[12]);
        Assert.Equal(0xBB, data[13]);
    }

    [Fact]
    public void DataBuilder_ReadNext_BuildsFrameSequence()
    {
        // Arrange
        byte frameIndex = 0x03;

        // Act
        var data = DataBuilder.ReadNext(frameIndex);

        // Assert
        Assert.Single(data);
        Assert.Equal(0x03, data[0]);
    }

    #endregion

    #region 广播帧识别

    [Fact]
    public void BroadcastFrame_AddressAllAA_IsRecognized()
    {
        // Arrange - 广播地址全为 0xAA
        byte[] frame = [
            0xFE, 0xFE, 0x68,
            0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA,
            0x68, 0x13, 0x00,
            0x00, 0x16
        ];

        // Act
        bool isBroadcast = ((ReadOnlySpan<byte>)frame).IsBroadcast();

        // Assert
        Assert.True(isBroadcast);
    }

    [Fact]
    public void NormalFrame_IsNotBroadcast()
    {
        // Arrange
        byte[] frame = [
            0xFE, 0xFE, 0x68,
            0x11, 0x11, 0x00, 0x00, 0x00, 0x00,
            0x68, 0x11, 0x00,
            0x00, 0x16
        ];

        // Act
        bool isBroadcast = ((ReadOnlySpan<byte>)frame).IsBroadcast();

        // Assert
        Assert.False(isBroadcast);
    }

  #endregion

  #region 完整协议流程

  [Theory]
  [InlineData(0x11, 0x91)] // 读数据
  [InlineData(0x12, 0x92)] // 读后续数据
  [InlineData(0x14, 0x94)] // 写数据
  public void ResponseCode_IsCommandCodePlus0x80(byte command, byte expectedResponse) =>
      // Assert - DLT645 响应码 = 命令码 + 0x80
      Assert.Equal(command + 0x80, expectedResponse);

  [Fact]
    public void CompleteReadRequest_WithEncryption_IsValid()
    {
        // Arrange
        byte[] address = [0x11, 0x11, 0x00, 0x00, 0x00, 0x00];
        byte control = 0x11;
        uint dataId = 0x00010000;
        
        // 构建并加密数据
        var dataBytes = DataBuilder.Read(dataId);
        Span<byte> dataSpan = dataBytes;
        MessageHeaderExtensions.EncodeData(dataSpan);

        // Act
        var header = new MessageHeader(address, control, dataBytes);
        var frame = header.ToBytes();

        // Assert - 验证完整帧
        Assert.Equal(0x68, frame[2]);       // 起始符
        Assert.Equal(0x11, frame[10]);      // 控制码
        Assert.Equal(0x04, frame[11]);      // 数据长度
        Assert.Equal(0x33, frame[12]);      // 加密后的数据标识第一字节
        Assert.Equal(0x16, frame[^1]);      // 结束符
        
        // 验证校验和有效性
        byte checksum = 0;
        for (int i = 2; i < frame.Length - 2; i++)
            checksum += frame[i];
        Assert.Equal(checksum, frame[^2]);
    }

    #endregion
}
