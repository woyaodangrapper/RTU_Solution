# Asprtu RTU Solution
面向 .NET 9 的多协议 RTU 通讯工具集，涵盖 DLT645 电力仪表、TCP 客户端/服务端以及内置的 BACnet 协议库。核心目标：提供可扩展、可观测、可测试的设备接入能力。

## 当前概览（正在做什么）
- DLT645 串口通信：自动串口参数协商、广播读地址、异步帧组装，见 [src/Asprtu.Rtu.DLT645](src/Asprtu.Rtu.DLT645)。
- TCP 通道：带长度头的消息分片/组装、连接状态跟踪、类型化 `TrySendAsync` 重载，见 [src/Asprtu.Rtu.TcpServer](src/Asprtu.Rtu.TcpServer) 和 [src/Asprtu.Rtu.TcpClient](src/Asprtu.Rtu.TcpClient)。
- BACnet 支撑库：内置第三方 BACnet 实现并对齐 .NET 9 依赖，见 [src/Asprtu.Rtu.BACnet](src/Asprtu.Rtu.BACnet)。
- 基础设施：依赖注入扩展、通道抽象与缓冲、消息队列工具，见 [src/Infrastructures/Asprtu.Rtu](src/Infrastructures/Asprtu.Rtu)。

## 目录速览
- [sample/Dlt645](sample/Dlt645)：串口读表示例，演示自动协商与广播读地址。
- [sample/Tcp](sample/Tcp)：本地回环的 TCP 客户端/服务端示例。
- [src/Asprtu.Rtu.DLT645](src/Asprtu.Rtu.DLT645)：DLT645 客户端实现、串口通道、消息封装与解析。
- [src/Asprtu.Rtu.TcpServer](src/Asprtu.Rtu.TcpServer) / [src/Asprtu.Rtu.TcpClient](src/Asprtu.Rtu.TcpClient)：TCP 通信栈。
- [src/Asprtu.Rtu.BACnet](src/Asprtu.Rtu.BACnet)：BACnet 协议库（MIT 授权）。
- [tests/Tcp](tests/Tcp)：TCP 相关测试用例（在补充中）。

## 快速开始
### DLT645 串口示例
基于示例程序 [sample/Dlt645/Program.cs](sample/Dlt645/Program.cs)：

```csharp
using Asprtu.Rtu.DLT645.Contracts;
using Microsoft.Extensions.Logging;

var logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

var channel = new CreateBuilder("MyChannel")
	.WithChannel("COM5")
	.WithLogger(logger)
	.Run();

await foreach (var frame in await channel.TryReadAddressAsync())
{
	Console.WriteLine($"Received address: {BitConverter.ToString(frame.Address)}");
}
```

### TCP 回环示例
简化自 [sample/Tcp/Program.cs](sample/Tcp/Program.cs)，演示同机服务器与客户端通信：

```csharp
var logger = LoggerFactory.Create(builder => builder.AddConsole());

var serverFactory = new TcpServerFactory(logger);
var server = serverFactory.CreateTcpServer(new("default", "127.0.0.1", 6688));
_ = server.TryExecuteAsync();

var clientFactory = new TcpClientFactory(logger);
var client = clientFactory.CreateTcpClient(new("default", "127.0.0.1", 6688));
_ = client.TryExecuteAsync();

await client.TrySendAsync(1);
```

## 未来方向（想做什么）
- 完成 DLT645 写入/多命令链路（`TrySendAsync`/`TryWriteAsync`）及帧验证路径的健壮性测试。
- 丰富串口自动协商：扩展候选波特率/校验组合，完善取消与超时语义的处理。
- 统一通道抽象与 DI 注册，降低多协议共存的接入成本。
- 增补 TCP/BACnet 的示例与端到端测试，接入 CI（构建、单测、代码分析）。
- 发布 NuGet 预览包与中文使用文档。

## 环境与依赖
- .NET 9.0（LangVersion=preview）。
- 串口：RJCP.SerialPortStream 3.0.4。
- 缓存/反应式：FusionCache、System.Reactive。

## 许可与第三方
- 仓库包含来自 ela-compil 的 [RTU.BACNET Library](https://github.com/ela-compil/BACnet/tree/master)（MIT）。
