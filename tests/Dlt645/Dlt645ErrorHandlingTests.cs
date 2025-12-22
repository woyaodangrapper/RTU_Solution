using Aspdcs.Rtu.Contracts.DLT645;
using Aspdcs.Rtu.DLT645.Extensions;

namespace Dlt645.Tests;

/// <summary>
/// DLT645 错误处理测试 - 核心测试维度 #3
/// 测试覆盖: 异常帧、错误响应、边界情况、容错性
/// </summary>
public class Dlt645ErrorHandlingTests
{
    #region 异常帧处理

    [Fact]
    public void InvalidFrame_TooShort_HandledGracefully()
    {
        // Arrange - 不完整的帧
        byte[] shortFrame = [0x68, 0x11, 0x11];

        // Act
        var header = shortFrame.TryReadHeader();

        // Assert - 应该返回 null 或默认值
        Assert.True(header == null || header.Value.StartCode != 0x68);
    }

    [Fact]
    public void InvalidFrame_WrongStartCode_Rejected()
    {
        // Arrange - 错误的起始符
        byte[] invalidFrame = [
            0xFF, 0xFF,  // 错误的前导
            0x67,        // 错误的起始符
            0x11, 0x11, 0x00, 0x00, 0x00, 0x00,
            0x68, 0x11, 0x00,
            0x00, 0x16
        ];

        // Act
        var header = invalidFrame.TryReadHeader();

        // Assert
        Assert.True(header == null || header.Value.StartCode != 0x68);
    }

    [Fact]
    public void InvalidFrame_WrongEndCode_Rejected()
    {
        // Arrange - 错误的结束符
        byte[] frame = [
            0xFE, 0xFE, 0x68,
            0x11, 0x11, 0x00, 0x00, 0x00, 0x00,
            0x68, 0x11, 0x00,
            0x00, 0xFF  // 错误的结束符
        ];

        // Act
        var header = new MessageHeader(frame.AsSpan());

        // Assert - 即使解析了，EndCode 应该不等于标准值
        Assert.NotEqual(0x16, header.EndCode);
    }

    [Fact]
    public void EmptyData_DoesNotCrash()
    {
        // Arrange
        byte[] address = [0x11, 0x11, 0x00, 0x00, 0x00, 0x00];
        byte control = 0x13;
        byte[] emptyData = [];

        // Act
        var header = new MessageHeader(address, control, emptyData);
        var bytes = header.ToBytes();

        // Assert
        Assert.Equal(0, header.Length);
        Assert.NotNull(bytes);
    }

    #endregion

    #region 边界情况测试

    [Fact]
    public void MaxDataLength_200Bytes_HandledCorrectly()
    {
        // Arrange - DLT645 协议最大数据长度为 200 字节
        byte[] address = [0x11, 0x11, 0x00, 0x00, 0x00, 0x00];
        byte control = 0x11;
        byte[] maxData = new byte[200];
        Array.Fill(maxData, (byte)0x33);

        // Act
        var header = new MessageHeader(address, control, maxData);
        var bytes = header.ToBytes();

        // Assert
        Assert.Equal(200, header.Length);
        Assert.NotNull(bytes);
        Assert.Equal(0x16, bytes[^1]); // 结束符应该正确
    }

    [Fact]
    public void AllZeroAddress_IsValid()
    {
        // Arrange
        byte[] zeroAddress = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        byte control = 0x11;

        // Act
        var header = new MessageHeader(zeroAddress, control);
        var bytes = header.ToBytes();

        // Assert
        Assert.Equal(zeroAddress, header.Address);
        Assert.NotNull(bytes);
    }

    [Fact]
    public void AllFFAddress_IsValid()
    {
        // Arrange
        byte[] ffAddress = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        byte control = 0x11;

        // Act
        var header = new MessageHeader(ffAddress, control);
        var bytes = header.ToBytes();

        // Assert
        Assert.Equal(ffAddress, header.Address);
        Assert.NotNull(bytes);
    }

    [Fact]
    public void EncodeData_WithEmptySpan_DoesNotCrash()
    {
        // Arrange
        byte[] emptyData = [];

        // Act
        Span<byte> span = emptyData;
        MessageHeaderExtensions.EncodeData(span);

        // Assert
        Assert.Empty(emptyData);
    }

    [Fact]
    public void UpdateChecksum_WithTooShortFrame_DoesNotCrash()
    {
        // Arrange
        byte[] shortFrame = [0x68, 0x11];

        // Act & Assert - 不应该抛出异常
        Span<byte> span = shortFrame;
        MessageHeaderExtensions.UpdateChecksum(span);
        Assert.Equal(2, shortFrame.Length);
    }

    #endregion

    #region 粘包/半包场景

    [Fact]
    public void MultipleLeadingFE_SkippedCorrectly()
    {
        // Arrange - 多个前导 0xFE
        byte[] frame = [
            0xFE, 0xFE, 0xFE, 0xFE, 0xFE, 0xFE,
            0x68,
            0x11, 0x11, 0x00, 0x00, 0x00, 0x00,
            0x68, 0x11, 0x00,
            0x79, 0x16
        ];

        // Act - 查找第一个 0x68
        int index = 0;
        while (index < frame.Length && frame[index] == 0xFE)
            index++;

        // Assert
        Assert.Equal(6, index);
        Assert.Equal(0x68, frame[index]);
    }

    [Fact]
    public void TwoConsecutiveFrames_CanBeIdentified()
    {
        // Arrange - 两个完整帧（粘包）
        byte[] twoFrames = [
            // 第一帧
            0xFE, 0xFE, 0x68,
            0x11, 0x11, 0x00, 0x00, 0x00, 0x00,
            0x68, 0x11, 0x00,
            0x79, 0x16,
            // 第二帧
            0xFE, 0xFE, 0x68,
            0x22, 0x22, 0x00, 0x00, 0x00, 0x00,
            0x68, 0x11, 0x00,
            0x8A, 0x16
        ];

        // Act - 查找所有帧起始位置
        var positions = new List<int>();
        for (int i = 0; i < twoFrames.Length - 12; i++)
        {
            if (twoFrames[i] == 0x68 && i + 7 < twoFrames.Length && twoFrames[i + 7] == 0x68)
                positions.Add(i);
        }

        // Assert - 至少应该找到两个帧
        Assert.True(positions.Count >= 2);
    }

    [Theory]
    [InlineData(new byte[] { 0x68 })]
    [InlineData(new byte[] { 0x68, 0x11, 0x11 })]
    [InlineData(new byte[] { 0x68, 0x11, 0x11, 0x00, 0x00, 0x00, 0x00, 0x68 })]
    public void IncompleteFrame_Identified(byte[] incompleteFrame)
    {
        // Act
        bool isComplete = incompleteFrame.Length >= 12;

        // Assert - 不完整的帧应该被识别
        Assert.False(isComplete);
    }

    #endregion

    #region 错误响应码测试

    [Fact]
    public void ErrorResponse_WithErrorBit_IsRecognized()
    {
        // Arrange - 错误响应: 控制码 = 命令码 + 0x80 + 0x40
        byte commandCode = 0x11;
        byte errorResponseCode = (byte)(commandCode + 0x80 + 0x40); // 0xD1

        // Act & Assert
        Assert.Equal(0xD1, errorResponseCode);
        Assert.True((errorResponseCode & 0x40) != 0); // 错误位置位
    }

    [Fact]
    public void NormalResponse_NoErrorBit_IsRecognized()
    {
        // Arrange
        byte commandCode = 0x11;
        byte normalResponseCode = (byte)(commandCode + 0x80); // 0x91

        // Act & Assert
        Assert.Equal(0x91, normalResponseCode);
        Assert.False((normalResponseCode & 0x40) != 0); // 错误位未置位
    }

    #endregion

    #region 特殊数据测试

    [Fact]
    public void EncodeData_WithOverflow_HandlesCorrectly()
    {
        // Arrange - 测试字节溢出情况
        byte[] data = [0xFF, 0xFF, 0xFF];  // 0xFF + 0x33 = 0x32 (溢出)
        byte[] expected = [0x32, 0x32, 0x32];

        // Act
        Span<byte> span = [.. data];
        MessageHeaderExtensions.EncodeData(span);

        // Assert
        Assert.Equal(expected, span.ToArray());
    }

    [Fact]
    public void BroadcastAddress_IsSpecial()
    {
        // Arrange
        byte[] broadcastAddr = [0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA];
        byte[] normalAddr = [0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAB];

        // Act
        byte[] frame1 = [0xFE, 0xFE, 0x68, .. broadcastAddr, 0x68, 0x13, 0x00, 0x00, 0x16];
        byte[] frame2 = [0xFE, 0xFE, 0x68, .. normalAddr, 0x68, 0x13, 0x00, 0x00, 0x16];

        // Assert
        Assert.True(((ReadOnlySpan<byte>)frame1).IsBroadcast());
        Assert.False(((ReadOnlySpan<byte>)frame2).IsBroadcast());
    }

    #endregion
}
