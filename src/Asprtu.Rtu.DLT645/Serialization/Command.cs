namespace Asprtu.Rtu.DLT645.Serialization;

[AttributeUsage(AttributeTargets.Enum)]
public sealed class EnumCommandAttribute : Attribute { }

/// <summary>
/// DLT645 协议命令定义（静态容器）
/// </summary>
public static class Command
{
    #region 电能量 (Energy Data) - 00 xx xx xx
    /// <summary>
    /// 电能量相关数据标识符
    /// </summary>
    [Flags, EnumCommand]
    public enum EnergyData
    {
        /// <summary>
        /// 组合有功总电能
        /// </summary>
        None = 0x00000000,

        /// <summary>
        /// 组合有功费率1电能
        /// </summary>
        CombinedActiveRate1Energy = 0x00000100,

        /// <summary>
        /// 正向有功总电能
        /// </summary>
        ForwardActiveTotalEnergy = 0x00010000,

        /// <summary>
        /// 正向有功费率1电能
        /// </summary>
        ForwardActiveRate1Energy = 0x00010100,

        /// <summary>
        /// 反向有功总电能
        /// </summary>
        ReverseActiveTotalEnergy = 0x00020000,

        /// <summary>
        /// 反向有功费率1电能
        /// </summary>
        ReverseActiveRate1Energy = 0x00020100,

        /// <summary>
        /// 组合无功1总电能
        /// </summary>
        CombinedReactive1TotalEnergy = 0x00030000,

        /// <summary>
        /// 组合无功2总电能
        /// </summary>
        CombinedReactive2TotalEnergy = 0x00040000,

        /// <summary>
        /// 第一象限无功总电能
        /// </summary>
        Quadrant1ReactiveTotalEnergy = 0x00050000,

        /// <summary>
        /// 第二象限无功总电能
        /// </summary>
        Quadrant2ReactiveTotalEnergy = 0x00060000,

        /// <summary>
        /// 第三象限无功总电能
        /// </summary>
        Quadrant3ReactiveTotalEnergy = 0x00070000,

        /// <summary>
        /// 第四象限无功总电能
        /// </summary>
        Quadrant4ReactiveTotalEnergy = 0x00080000,

        /// <summary>
        /// 正向视在总电能
        /// </summary>
        ForwardApparentTotalEnergy = 0x00090000,

        /// <summary>
        /// 反向视在总电能
        /// </summary>
        ReverseApparentTotalEnergy = 0x000A0000
    }
    #endregion

    #region 最大需量 (Maximum Demand) - 01 xx xx xx
    /// <summary>
    /// 最大需量数据标识符
    /// </summary>
    [EnumCommand]
    public enum MaximumDemand
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// 正向有功总最大需量
        /// </summary>
        ForwardActiveTotalMaxDemand = 0x01010000,

        /// <summary>
        /// 反向有功总最大需量
        /// </summary>
        ReverseActiveTotalMaxDemand = 0x01020000
    }
    #endregion

    #region 瞬时量/变量 (Variables) - 02 xx xx xx
    /// <summary>
    /// 瞬时量/变量数据标识符
    /// </summary>
    [EnumCommand]
    public enum Variables
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// A相电压
        /// </summary>
        VoltagePhaseA = 0x02010100,

        /// <summary>
        /// B相电压
        /// </summary>
        VoltagePhaseB = 0x02010200,

        /// <summary>
        /// C相电压
        /// </summary>
        VoltagePhaseC = 0x02010300,

        /// <summary>
        /// 电压数据块
        /// </summary>
        VoltageDataBlock = 0x020101FF,

        /// <summary>
        /// A相电流
        /// </summary>
        CurrentPhaseA = 0x02020100,

        /// <summary>
        /// B相电流
        /// </summary>
        CurrentPhaseB = 0x02020200,

        /// <summary>
        /// C相电流
        /// </summary>
        CurrentPhaseC = 0x02020300,

        /// <summary>
        /// 电流数据块
        /// </summary>
        CurrentDataBlock = 0x020201FF,

        /// <summary>
        /// 瞬时总有功功率
        /// </summary>
        InstantaneousActivePowerTotal = 0x02030000,

        /// <summary>
        /// A相瞬时有功功率
        /// </summary>
        InstantaneousActivePowerPhaseA = 0x02030100,

        /// <summary>
        /// B相瞬时有功功率
        /// </summary>
        InstantaneousActivePowerPhaseB = 0x02030200,

        /// <summary>
        /// C相瞬时有功功率
        /// </summary>
        InstantaneousActivePowerPhaseC = 0x02030300,

        /// <summary>
        /// 瞬时有功功率数据块
        /// </summary>
        InstantaneousActivePowerDataBlock = 0x020300FF,

        /// <summary>
        /// 瞬时总无功功率
        /// </summary>
        InstantaneousReactivePowerTotal = 0x02040000,

        /// <summary>
        /// A相瞬时无功功率
        /// </summary>
        InstantaneousReactivePowerPhaseA = 0x02040100,

        /// <summary>
        /// B相瞬时无功功率
        /// </summary>
        InstantaneousReactivePowerPhaseB = 0x02040200,

        /// <summary>
        /// C相瞬时无功功率
        /// </summary>
        InstantaneousReactivePowerPhaseC = 0x02040300,

        /// <summary>
        /// 瞬时无功功率数据块
        /// </summary>
        InstantaneousReactivePowerDataBlock = 0x020400FF,

        /// <summary>
        /// 瞬时总视在功率
        /// </summary>
        InstantaneousApparentPowerTotal = 0x02050000,

        /// <summary>
        /// A相瞬时视在功率
        /// </summary>
        InstantaneousApparentPowerPhaseA = 0x02050100,

        /// <summary>
        /// B相瞬时视在功率
        /// </summary>
        InstantaneousApparentPowerPhaseB = 0x02050200,

        /// <summary>
        /// C相瞬时视在功率
        /// </summary>
        InstantaneousApparentPowerPhaseC = 0x02050300,

        /// <summary>
        /// 瞬时视在功率数据块
        /// </summary>
        InstantaneousApparentPowerDataBlock = 0x020500FF,

        /// <summary>
        /// 总功率因数
        /// </summary>
        PowerFactorTotal = 0x02060000,

        /// <summary>
        /// A相功率因数
        /// </summary>
        PowerFactorPhaseA = 0x02060100,

        /// <summary>
        /// B相功率因数
        /// </summary>
        PowerFactorPhaseB = 0x02060200,

        /// <summary>
        /// C相功率因数
        /// </summary>
        PowerFactorPhaseC = 0x02060300,

        /// <summary>
        /// 功率因数数据块
        /// </summary>
        PowerFactorDataBlock = 0x020600FF
    }
    #endregion

    #region 参变量 (Parameters) - 04 xx xx xx
    /// <summary>
    /// 参变量数据标识符
    /// </summary>
    [EnumCommand]
    public enum Parameters
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// 需量数据
        /// </summary>
        DemandData = 0x04000101,

        /// <summary>
        /// A相需量
        /// </summary>
        DemandPhaseA = 0x04000201,

        /// <summary>
        /// B相需量
        /// </summary>
        DemandPhaseB = 0x04000202,

        /// <summary>
        /// C相需量
        /// </summary>
        DemandPhaseC = 0x04000203,

        /// <summary>
        /// 密码认证
        /// </summary>
        PasswordAuthentication = 0x04000401
    }
    #endregion

    #region 冻结类型 (Freeze Types) - 07 xx xx xx
    /// <summary>
    /// DLT645 冻结类型枚举
    /// Freeze Type Enumeration
    /// </summary>
    [EnumCommand]
    public enum Freeze
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// 瞬时冻结 - Instant Freeze
        /// 立即冻结当前数据
        /// </summary>
        Instant = 0x01,

        /// <summary>
        /// 时冻结 - Hourly Freeze
        /// 按小时周期冻结数据
        /// </summary>
        Hourly = 0x02,

        /// <summary>
        /// 日冻结 - Daily Freeze
        /// 按日周期冻结数据
        /// </summary>
        Daily = 0x03,

        /// <summary>
        /// 月冻结 - Monthly Freeze
        /// 按月周期冻结数据
        /// </summary>
        Monthly = 0x04
    }
    #endregion

    #region DLT645-1997 兼容 (1997 Compatibility)
    /// <summary>
    /// DLT645-1997 兼容数据标识符
    /// </summary>
    [EnumCommand]
    public enum Legacy1997
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// 总有功电能
        /// </summary>
        TotalActiveEnergy = 0x9010,

        /// <summary>
        /// 日最大需量
        /// </summary>
        DailyMaxDemand = 0x0001
    }
    #endregion

    #region 控制代码  

    /// <summary>
    /// DLT645 协议控制代码
    /// </summary>
    [EnumCommand]
    public enum Code
    {
        /// <summary>
        /// 0. 无
        /// </summary>
        None = 0x00,

        // ------------------- 主站请求帧 -------------------

        /// <summary>
        /// 1. 安全认证
        ///     主站请求：0x03
        ///     从站正常应答：0x83
        ///     从站异常应答：0xC3
        /// </summary>
        SecurityAuthentication = 0x03,
        SecurityAuthenticationAck = 0x83,
        SecurityAuthenticationError = 0xC3,

        /// <summary>
        /// 2. 广播校时
        ///     主站请求：0x08
        ///     不要求从站应答
        /// </summary>
        BroadcastTimeCalibration = 0x08,


        /// <summary>
        /// 3. 读数据
        ///     主站请求：0x11
        ///     从站正常应答：0x91 无后续数据 / 0xB1 有后续数据
        ///     从站异常应答：0xD1
        /// </summary>
        ReadData = 0x11,
        ReadDataAck = 0x91,
        ReadDataWithSubsequent = 0xB1,
        ReadDataError = 0xD1,

        /// <summary>
        /// 4. 读后续数据
        ///     主站请求：0x12
        ///     从站正常应答：0x92 无后续数据 / 0xB2 有后续数据
        ///     从站异常应答：0xD2
        /// </summary>
        ReadSubsequentData = 0x12,
        ReadSubsequentDataAck = 0x92,
        ReadSubsequentDataWithMore = 0xB2,
        ReadSubsequentDataError = 0xD2,

        /// <summary>
        /// 5. 读通信地址
        ///     主站请求：0x13
        ///     从站正常应答：0x93
        ///     异常不应答
        /// </summary>
        ReadAddress = 0x13,
        ReadAddressAck = 0x93,

        /// <summary>
        /// 6. 写数据
        ///     主站请求：0x14
        ///     从站正常应答：0x94
        ///     从站异常应答：0xD4
        /// </summary>
        WriteData = 0x14,
        WriteDataAck = 0x94,
        WriteDataError = 0xD4,

        /// <summary>
        /// 7. 写通信地址
        ///     主站请求：0x15
        ///     从站正常应答：0x95
        /// </summary>
        WriteAddress = 0x15,
        WriteAddressAck = 0x95,

        /// <summary>
        /// 8. 冻结命令
        ///     主站请求：0x16
        ///     从站正常应答：0x96
        ///     从站异常应答：0xD6
        /// </summary>
        Freeze = 0x16,
        FreezeAck = 0x96,
        FreezeError = 0xD6,

        /// <summary>
        /// 9. 更改通信速率
        ///     主站请求：0x17
        ///     从站正常应答：0x97
        ///     从站异常应答：0xD7
        /// </summary>
        ChangeBaudRate = 0x17,
        ChangeBaudRateAck = 0x97,
        ChangeBaudRateError = 0xD7,

        /// <summary>
        /// 10. 修改密码
        ///     主站请求：0x18
        ///     从站正常应答：0x98
        ///     从站异常应答：0xD8
        /// </summary>
        ModifyPassword = 0x18,
        ModifyPasswordAck = 0x98,
        ModifyPasswordError = 0xD8,

        /// <summary>
        /// 11. 最大需量清零
        ///     主站请求：0x19
        ///     从站正常应答：0x99
        ///     从站异常应答：0xD9
        /// </summary>
        ClearMaxDemand = 0x19,
        ClearMaxDemandAck = 0x99,
        ClearMaxDemandError = 0xD9,

        /// <summary>
        /// 12. 电表清零
        ///     主站请求：0x1A
        ///     从站正常应答：0x9A
        ///     从站异常应答：0xDA
        /// </summary>
        ClearMeter = 0x1A,
        ClearMeterAck = 0x9A,
        ClearMeterError = 0xDA,

        /// <summary>
        /// 13. 事件清零
        ///     主站请求：0x1B
        ///     从站正常应答：0x9B
        ///     从站异常应答：0xDB
        /// </summary>
        ClearEvents = 0x1B,
        ClearEventsAck = 0x9B,
        ClearEventsError = 0xDB,

        /// <summary>
        /// 14. 跳合闸、报警、保电
        ///     主站请求：0x1C
        ///     从站正常应答：0x9C
        ///     从站异常应答：0xDC
        /// </summary>
        RelayControl = 0x1C,
        RelayControlAck = 0x9C,
        RelayControlError = 0xDC,

        /// <summary>
        /// 15. 多功能端子输出控制
        ///     主站请求：0x1D
        ///     从站正常应答：0x9D
        ///     从站异常应答：0xDD
        /// </summary>
        TerminalControl = 0x1D,
        TerminalControlAck = 0x9D,
        TerminalControlError = 0xDD,
    }
    #endregion
}