# Aspdcs RTU Solution

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

生产就绪的多协议 RTU 通讯库，支持 .NET 6+ / .NET Framework 4.6.2+

[English](README.en.md) | 简体中文

## ✨ 核心特性

- **[Aspdcs.Rtu.DLT645](src/Aspdcs.Rtu.DLT645)** - DLT645-2007 电力仪表通信
  - ✅ 完整协议实现（±0x33 加解密、BCD 解码、校验和）
  - ✅ 零拷贝架构（性能提升 20-40%）
  - ✅ 智能响应（单播快速返回、广播自适应）
  - 📝 转译并完善自 [WKJay/DLT645](https://github.com/WKJay/DLT645)

- **[Aspdcs.Rtu.TcpServer](src/Aspdcs.Rtu.TcpServer)** / **[TcpClient](src/Aspdcs.Rtu.TcpClient)** - TCP 通信栈
  - ✅ 粘包/半包处理
  - ✅ 长度头协议
  - ✅ 异步高性能

- **[Aspdcs.Rtu.BACnet](src/Aspdcs.Rtu.BACnet)** - BACnet 协议库（MIT License）

- **[Aspdcs.Rtu](src/Infrastructures/Aspdcs.Rtu)** - 基础设施
  - 依赖注入支持
  - 通道抽象
  - 环形缓冲区

## 快速开始

### 安装

```bash
dotnet add package Aspdcs.Rtu.DLT645
```

### 示例代码

```csharp
using Aspdcs.Rtu.DLT645;
using Microsoft.Extensions.Logging;

// 创建客户端
var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddConsole());

var options = new ChannelOptions.CreateBuilder("Meter")
    .WithChannel("COM5", 2400, Parity.Even)
    .WithTimeout(TimeSpan.FromSeconds(2))
    .Build();

using var client = new Dlt645Client(options, loggerFactory);

// 读取电能数据
byte[] address = { 0x11, 0x11, 0x00, 0x00, 0x00, 0x00 };
uint dataId = 0x00010000; // 当前正向有功总电能

await foreach (var value in client.ReadAsync(address, dataId))
{
    Console.WriteLine($"电能: {value}");
}
```

## 📚 文档与示例

- [DLT645 完整文档](src/Aspdcs.Rtu.DLT645/README.md)
- [DLT645 示例代码](sample/Dlt645)
- [TCP 示例代码](sample/Tcp)
- [协议完成度评估](docs/LICENSE/DLT645_协议栈完成度.md)

## 许可证

MIT License - 包含第三方 [BACnet 库](https://github.com/ela-compil/BACnet)（MIT）
