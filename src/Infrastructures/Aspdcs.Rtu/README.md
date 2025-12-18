# Aspdcs.Rtu

面向 .NET 9 的 RTU 通讯基础设施库，为 DLT645、TCP、BACnet 等协议提供统一的抽象层和工具集。

## 功能特性

- **通道抽象** - 统一的通信通道接口，支持串口、TCP、UDP 等多种传输方式
- **消息队列** - 异步消息处理队列，支持背压控制和优先级调度
- **依赖注入** - 完整的 DI 扩展，便于集成到 ASP.NET Core 或主机服务
- **缓存支持** - 基于 FusionCache 的高性能分布式缓存抽象
- **响应式扩展** - 集成 System.Reactive，支持响应式编程模式

## 快速开始

```csharp
using Aspdcs.Rtu.Contracts;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddRtuServices(); // 注册 RTU 基础服务

var provider = services.BuildServiceProvider();
```

## 包含组件

- `Contracts/` - 核心接口与契约定义
- `Attributes/` - 特性标记（如 LibraryCapacities）
- `Extensions/` - 扩展方法集合
- `Queue/` - 消息队列实现

## 依赖项

- Microsoft.Extensions.Hosting.Abstractions
- Microsoft.Extensions.Caching.Memory
- ZiggyCreatures.FusionCache
- System.Reactive

## 授权

MIT License - Copyright © 2025 Aspdcs
