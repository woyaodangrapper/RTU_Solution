using System.Buffers.Binary;
using static Aspdcs.Rtu.DLT645.Serialization.DataFormats;

namespace Aspdcs.Rtu.DLT645.Serialization;

public abstract record SemanticValue(string Identifier, string Format, string Unit, string? Custom = null);

public sealed record AddressValue(string Address);

public sealed record NumericValue(string Identifier, decimal Value, string Format, string Unit, string? Custom = null)
    : SemanticValue(Identifier, Format, Unit, Custom);

public sealed record DataFormat(string Name, string Unit, string Format, int Exponent, ValueEncoding Encoding, int Length);

public static class DataFormats
{
    public enum ValueEncoding
    {
        Bcd,
        DateTime
    }

    private static readonly Dictionary<uint, DataFormat> _map
            = new()
            {
                // 电压数据 (格式: XXX.X V, 2字节)
                {
                    (uint)Command.Variables.VoltagePhaseA,
                    new("A相电压", "V", "XXX.X", -1, ValueEncoding.Bcd, 2)
                },
                {
                    (uint)Command.Variables.VoltagePhaseB,
                    new("B相电压", "V", "XXX.X", -1, ValueEncoding.Bcd, 2)
                },
                {
                    (uint)Command.Variables.VoltagePhaseC,
                    new("C相电压", "V", "XXX.X", -1, ValueEncoding.Bcd, 2)
                },

                // 电流数据 (格式: XXX.XXX A, 3字节)
                {
                    ((uint)Command.Variables.CurrentPhaseA),
                    new( "A相电流","A", "XXX.XXX", -3, ValueEncoding.Bcd ,3)
                },{
                    ((uint)Command.Variables.CurrentPhaseB),
                    new( "B相电流","A", "XXX.XXX", -3, ValueEncoding.Bcd ,3)
                },{
                    ((uint)Command.Variables.CurrentPhaseC),
                    new( "C相电流","A", "XXX.XXX", -3, ValueEncoding.Bcd ,3)
                },

                // 有功功率数据 (格式: XX.XXXX kW, 3字节)
                {
                    ((uint)Command.Variables.InstantaneousActivePowerPhaseA),
                    new("A相有功功率","kW", "XX.XXXX", -4, ValueEncoding.Bcd ,3)
                },{
                    ((uint)Command.Variables.InstantaneousActivePowerPhaseB),
                    new("B相有功功率","kW", "XX.XXXX", -4, ValueEncoding.Bcd ,3)
                },{
                    ((uint)Command.Variables.InstantaneousActivePowerPhaseC),
                    new("C相有功功率","kW", "XX.XXXX", -4, ValueEncoding.Bcd ,3)
                },{
                    ((uint)Command.Variables.InstantaneousActivePowerTotal),
                    new("总有功功率","kW", "XX.XXXX", -4, ValueEncoding.Bcd ,3)
                },

                // 无功功率数据 (格式: XX.XXXX kvar, 3字节)
                {
                    ((uint)Command.Variables.InstantaneousReactivePowerPhaseA),
                    new("A相无功功率","kvar", "XX.XXXX", -4, ValueEncoding.Bcd ,3)
                },{
                    ((uint)Command.Variables.InstantaneousReactivePowerPhaseB),
                    new("B相无功功率","kvar", "XX.XXXX", -4, ValueEncoding.Bcd ,3)
                },{
                    ((uint)Command.Variables.InstantaneousReactivePowerPhaseC),
                    new("C相无功功率","kvar", "XX.XXXX", -4, ValueEncoding.Bcd ,3)
                },{
                    ((uint)Command.Variables.InstantaneousReactivePowerTotal),
                    new("总无功功率","kvar", "XX.XXXX", -4, ValueEncoding.Bcd ,3)
                },

                // 视在功率数据 (格式: XX.XXXX kVA, 3字节)
                {
                    ((uint)Command.Variables.InstantaneousApparentPowerPhaseA),
                    new("A相视在功率","kVA", "XX.XXXX", -4, ValueEncoding.Bcd ,3)
                },{
                    ((uint)Command.Variables.InstantaneousApparentPowerPhaseB),
                    new("B相视在功率","kVA", "XX.XXXX", -4, ValueEncoding.Bcd ,3)
                },{
                    ((uint)Command.Variables.InstantaneousApparentPowerPhaseC),
                    new("C相视在功率","kVA", "XX.XXXX", -4, ValueEncoding.Bcd ,3)
                },{
                    ((uint)Command.Variables.InstantaneousApparentPowerTotal),
                    new("总视在功率","kVA", "XX.XXXX", -4, ValueEncoding.Bcd ,3)
                },

                // 功率因数 (格式: X.XXX, 2字节)
                {
                    ((uint)Command.Variables.PowerFactorPhaseA),
                    new("A相功率因数","", "X.XXX", -3, ValueEncoding.Bcd ,2)
                },{
                    ((uint)Command.Variables.PowerFactorPhaseB),
                    new("B相功率因数","", "X.XXX", -3, ValueEncoding.Bcd ,2)
                },{
                    ((uint)Command.Variables.PowerFactorPhaseC),
                    new("C相功率因数","", "X.XXX", -3, ValueEncoding.Bcd ,2)
                },{
                    ((uint)Command.Variables.PowerFactorTotal),
                    new("总功率因数","", "X.XXX", -3, ValueEncoding.Bcd ,2)
                },

                // 电能数据 (格式: XXXXXX.XX kWh/kvarh, 4字节)
                {
                    ((uint)Command.EnergyData.None),
                    new("组合有功总电能","kWh", "XXXXXX.XX", 0, ValueEncoding.Bcd ,4)
                },{
                    ((uint)Command.EnergyData.ForwardActiveTotalEnergy),
                    new("正向有功总电能","kWh", "XXXXXX.XX", 0, ValueEncoding.Bcd ,4)
                },{
                    ((uint)Command.EnergyData.ReverseActiveTotalEnergy),
                    new("反向有功总电能","kWh", "XXXXXX.XX", 0, ValueEncoding.Bcd ,4)
                },{
                    ((uint)Command.EnergyData.CombinedActiveRate1Energy),
                    new("组合有功费率1电能","kWh", "XXXXXX.XX", 0, ValueEncoding.Bcd ,4)
                },{
                    ((uint)Command.EnergyData.CombinedReactive1TotalEnergy),
                    new("组合无功1总电能","kWh", "XXXXXX.XX", 0, ValueEncoding.Bcd ,4)
                },{
                    ((uint)Command.EnergyData.CombinedReactive2TotalEnergy),
                    new("组合无功2总电能","kWh", "XXXXXX.XX", 0, ValueEncoding.Bcd ,4)
                },{
                    ((uint)Command.EnergyData.Quadrant1ReactiveTotalEnergy),
                    new("第一象限无功总电能","kWh", "XXXXXX.XX", 0, ValueEncoding.Bcd ,4)
                },{
                    ((uint)Command.EnergyData.Quadrant2ReactiveTotalEnergy),
                    new("第二象限无功总电能","kWh", "XXXXXX.XX", 0, ValueEncoding.Bcd ,4)
                },{
                    ((uint)Command.EnergyData.Quadrant3ReactiveTotalEnergy),
                    new("第三象限无功总电能","kWh", "XXXXXX.XX", 0, ValueEncoding.Bcd ,4)
                },{
                    ((uint)Command.EnergyData.Quadrant4ReactiveTotalEnergy),
                    new("第四象限无功总电能","kWh", "XXXXXX.XX", 0, ValueEncoding.Bcd ,4)
                },

                // DLT645-1997 总有功电能 (格式: XXXXXX.XX kWh, 4字节)
                {
                    ((uint)Command.Legacy1997.TotalActiveEnergy),
                    new("总有功电能","kWh", "XXXXXX.XX", 0, ValueEncoding.Bcd ,4)
                },

                // 默认未知格式
            };
    /// <summary>
    /// 从ID读取数据格式定义
    /// </summary>
    /// <param name="di"></param>
    /// <param name="def"></param>
    /// <returns></returns>
    public static bool TryGet(uint di, out DataFormat? def)
        => _map.TryGetValue(di, out def);

    /// <summary>
    /// 从数据域的ID读取数据格式定义
    /// </summary>
    /// <param name="data"></param>
    /// <param name="def"></param>
    /// <returns></returns>
    public static bool TryGet(ReadOnlySpan<byte> data, out DataFormat? def)
        => _map.TryGetValue(BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(0, 4)), out def);
}