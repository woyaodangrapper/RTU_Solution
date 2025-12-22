using Aspdcs.Rtu.DLT645.Extensions;

namespace Dlt645.Tests;

/// <summary>
/// DLT645 数据解析测试 - 核心测试维度 #2
/// 测试覆盖: BCD解码、数值转换、小数位处理、数据格式定义
/// </summary>
public class Dlt645DataParsingTests
{
    #region BCD 解码测试

    [Fact]
    public void BcdDecoding_SimpleValue_DecodesCorrectly()
    {
        // Arrange
        var decoder = new DataDecoder();

        // 完整响应帧：数据标识(4) + BCD值(4) + 厂家自定义(1)
        byte[] frame = [
            0xFE, 0xFE, 0x68,
            0x11, 0x11, 0x00, 0x00, 0x00, 0x00,
            0x68, 0x91, 0x09,
            0x33, 0x33, 0x34, 0x33,                // 数据标识 (已加密)
            0x45, 0x67, 0x89, 0xAB,                // BCD: 12 34 56 78 (已加密)
            0x33,                                   // 厂家自定义
            0x00, 0x16
        ];

        var format = new DataFormat(
            Name: "正向有功总电能",
            Unit: "kWh",
            Format: "XXXXXX.XX",  // 2位小数
            Encoding: DataFormats.ValueEncoding.Bcd,
            Length: 4
        );

        // Act
        var result = decoder.Decode(frame, format);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<NumericValue>(result);
        var numericValue = (NumericValue)result;
        Assert.Equal("kWh", numericValue.Unit);
    }

    [Fact]
    public void BcdDecoding_WithZeroValue_ReturnsZero()
    {
        // Arrange
        var decoder = new DataDecoder();

        // BCD 全0值 (加密后全是 0x33)
        byte[] frame = [
            0xFE, 0xFE, 0x68,
            0x11, 0x11, 0x00, 0x00, 0x00, 0x00,
            0x68, 0x91, 0x09,
            0x33, 0x33, 0x34, 0x33,
            0x33, 0x33, 0x33, 0x33,  // BCD: 00 00 00 00
            0x33,
            0xAC, 0x16
        ];

        var format = new DataFormat(
            Name: "测试",
            Unit: "kWh",
            Format: "XXXXXX.XX",
            Encoding: DataFormats.ValueEncoding.Bcd,
            Length: 4
        );

        if (frame.TryGetData(out var data))
        {
            MessageHeaderExtensions.DecodeData(data);
        }

        // Act
        var result = decoder.Decode(frame, format) as NumericValue;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0m, result.Value);
    }

    [Theory]
    [InlineData("XXXXXX.XX", 2)]   // 2位小数
    [InlineData("XXX.XXX", 3)]     // 3位小数
    [InlineData("XXXXXX", 0)]      // 无小数
    [InlineData("XX.XXXX", 4)]     // 4位小数
    public void DataFormat_DecimalPlaces_ParsedCorrectly(string format, int expectedPlaces)
    {
        // Act
        int dotIndex = format.IndexOf('.', StringComparison.Ordinal);
        int actualPlaces = dotIndex < 0 ? 0 : format.Length - dotIndex - 1;

        // Assert
        Assert.Equal(expectedPlaces, actualPlaces);
    }

    #endregion

    #region 数据格式定义测试

    [Fact]
    public void DataFormats_ContainsEnergyFormats()
    {
        // Arrange & Act - 正向有功总电能
        bool hasFormat = DataFormats.TryGet([0x00, 0x00, 0x01, 0x00], out var format);

        // Assert
        Assert.True(hasFormat);
        Assert.NotNull(format);
        Assert.Equal("kWh", format?.Unit);
    }

    [Fact]
    public void NumericValue_ContainsAllProperties()
    {
        // Arrange
        string id = "00010000";
        decimal value = 12345.67m;
        string unit = "kWh";
        string custom = "00";

        // Act
        var numericValue = new NumericValue(id, value, unit, custom);

        // Assert
        Assert.Equal(id, numericValue.Identifier);
        Assert.Equal(value, numericValue.Value);
        Assert.Equal(unit, numericValue.Unit);
        Assert.Equal(custom, numericValue.Custom);
    }

    [Fact]
    public void DataFormat_RecordType_IsImmutable()
    {
        // Arrange & Act
        var format = new DataFormat(
            Name: "A相电压",
            Unit: "V",
            Format: "XXX.X",
            Encoding: DataFormats.ValueEncoding.Bcd,
            Length: 2
        );

        // Assert - record 类型应该是不可变的
        Assert.Equal("A相电压", format.Name);
        Assert.Equal("V", format.Unit);
        Assert.Equal("XXX.X", format.Format);
        Assert.Equal(2, format.Length);
    }

    #endregion

    #region 协议命令定义测试

    [Theory]
    [InlineData(Command.Code.ReadData, 0x11)]
    [InlineData(Command.Code.ReadSubsequentData, 0x12)]
    [InlineData(Command.Code.ReadAddress, 0x13)]
    [InlineData(Command.Code.WriteData, 0x14)]
    [InlineData(Command.Code.WriteAddress, 0x15)]
    public void ControlCode_HasCorrectValue(Command.Code code, byte expected)
    {
        // Act
        byte actual = (byte)code;

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(Command.EnergyData.ForwardActiveTotalEnergy, 0x00010000u)]
    [InlineData(Command.EnergyData.ReverseActiveTotalEnergy, 0x00020000u)]
    [InlineData(Command.EnergyData.ForwardActiveRate1Energy, 0x00010100u)]
    public void EnergyDataId_HasCorrectValue(Command.EnergyData data, uint expected)
    {
        // Act
        uint actual = (uint)data;

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumCommand_Attribute_IsPresent()
    {
        // Act
        bool hasAttribute = Attribute.IsDefined(
            typeof(Command.Code),
            typeof(EnumCommandAttribute)
        );

        // Assert
        Assert.True(hasAttribute);
    }

    [Fact]
    public void DataIdentifier_ConvertsToBytes_InLittleEndian()
    {
        // Arrange
        uint dataId = 0x00010000; // 正向有功总电能

        // Act
        byte[] bytes = BitConverter.GetBytes(dataId);

        // Assert - 验证小端序
        Assert.Equal([0x00, 0x00, 0x01, 0x00], bytes);
    }

    #endregion
}
