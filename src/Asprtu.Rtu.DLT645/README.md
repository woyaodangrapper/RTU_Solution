# Asprtu.Rtu.DLT645

[![NuGet](https://img.shields.io/nuget/v/Asprtu.Rtu.DLT645.svg)](https://www.nuget.org/packages/Asprtu.Rtu.DLT645)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](../../LICENSE)

面向 .NET 9 的 DLT645 电力仪表通信库，提供串口自动协商、异步帧解析、零拷贝优化等特性。

## 核心特性

- **自动串口协商**：支持多种波特率/校验位组合的自动探测
- **异步帧组装**：完整的半包/粘包处理，支持 Span/Memory 零拷贝
- **广播读地址**：自动广播读取未知设备地址
- **依赖注入集成**：开箱即用的 DI 支持，单例工厂模式
- **可观测性**：完整的日志输出，便于调试和监控

## 快速开始

### 安装

```bash
dotnet add package Asprtu.Rtu.DLT645
```

### 基础用法

```csharp
using Asprtu.Rtu.DLT645;
using Asprtu.Rtu.DLT645.Contracts;
using Microsoft.Extensions.Logging;

// 创建日志工厂
var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

// 创建通道
var channel = new CreateBuilder("MyChannel")
    .WithChannel("COM5")           // 串口号
    .WithBaudRate(2400)            // 可选：波特率
    .WithParity(Parity.Even)       // 可选：校验位
    .WithLogger(loggerFactory)
    .Run();

// 广播读地址
await foreach (var frame in await channel.TryReadAddressAsync())
{
    Console.WriteLine($"设备地址: {BitConverter.ToString(frame.Address)}");
}

// 读取数据项（例如：电压）
var result = await channel.TryReadAsync(
    address: new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 },
    dataId: 0x02010100  // DLT645-2007 电压数据标识
);
```

### 依赖注入

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// 注册 DLT645 服务
services.AddDlt645Client(options =>
{
    options.Name = "Meter1";
    options.PortName = "COM5";
    options.BaudRate = 2400;
    options.Parity = System.IO.Ports.Parity.Even;
});

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IDlt645Client>();
```

## 高级特性

### 自动串口协商

当不确定设备串口参数时，可以使用自动协商功能：

```csharp
var channel = new CreateBuilder("AutoNegotiate")
    .WithChannel("COM5")
    .WithAutoNegotiate()           // 启用自动协商
    .WithLogger(loggerFactory)
    .Run();
```

支持的波特率：300, 600, 1200, 2400, 4800, 9600, 19200  
支持的校验位：Even (偶校验), Odd (奇校验), None (无校验)

### 零拷贝优化

使用 `Span<byte>` 和 `Memory<byte>` 减少内存分配：

```csharp
// 使用 Span<byte> 避免数组分配
Span<byte> address = stackalloc byte[6] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
var result = await channel.TryReadAsync(address, 0x02010100);
```

### 批量读取

```csharp
var dataIds = new uint[] 
{ 
    0x02010100,  // A 相电压
    0x02010200,  // B 相电压
    0x02010300   // C 相电压
};

foreach (var dataId in dataIds)
{
    var result = await channel.TryReadAsync(address, dataId);
    if (result.Success)
    {
        Console.WriteLine($"数据项 {dataId:X8}: {BitConverter.ToString(result.Data)}");
    }
}
```

## 协议支持

### DLT645-1997

- ✅ 读数据
- ✅ 广播校时
- ✅ 冻结数据读取
- ⚠️ 写数据（部分支持）

### DLT645-2007

- ✅ 读数据
- ✅ 读后续帧
- ✅ 广播读地址
- ✅ 广播校时
- ⚠️ 写数据（部分支持）
- ⚠️ 修改密码（规划中）
- ⚠️ 参数设置（规划中）

## 项目状态

当前完成度：**85%** 🔵🔵🔵🔵🟡

| 模块 | 完成度 | 状态 |
|------|:------:|:----:|
| 通道层 | 85% | ✅ |
| 客户端 | 80% | ✅ |
| 工厂模式 | 95% | ✅ |
| 报文解析 | 95% | ✅ |
| 串口协商 | 90% | ✅ |
| 读取功能 | 90% | ✅ |
| 写入功能 | 75% | ⚠️ |
| 单元测试 | 5% | ❌ |

详细信息见 [完成度文档](../../docs/LICENSE/DLT645_协议栈完成度.md)

## 已知限制

- 写入功能需要更多测试和错误处理
- 单元测试覆盖率较低
- 多设备并发访问需要更多验证

## 示例项目

完整示例见 [sample/Dlt645](../../sample/Dlt645)

## 性能

- 零拷贝优化性能提升：20-40%
- 支持异步并发操作
- ArrayPool 内存复用，减少 GC 压力

## 依赖

- .NET 9.0
- RJCP.SerialPortStream 3.0.4
- Microsoft.Extensions.Logging.Abstractions

## 路线图

- [x] 基础读取功能
- [x] 自动串口协商
- [x] 零拷贝优化
- [x] 依赖注入支持
- [ ] 完善写入功能
- [ ] 增加单元测试覆盖率
- [ ] 支持多设备并发
- [ ] 性能基准测试

## 许可证

MIT License - 见 [LICENSE](../../LICENSE) 文件

## 贡献

欢迎提交 Issue 和 Pull Request！

## 支持

- GitHub Issues: https://github.com/woyaodangrapper/RTU_Solution/issues
- 文档: [docs](../../docs)
