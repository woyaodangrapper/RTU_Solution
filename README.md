# Asprtu RTU Solution

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)

面向 .NET 9 的多协议 RTU 通讯工具集，支持 DLT645、TCP、BACnet 等工业协议。

[English](README.en.md) | 简体中文

## 核心组件

- **[Asprtu.Rtu.DLT645](src/Asprtu.Rtu.DLT645)** - DLT645 电力仪表通信（串口自动协商、异步帧解析）
- **[Asprtu.Rtu.TcpServer](src/Asprtu.Rtu.TcpServer)** / **[TcpClient](src/Asprtu.Rtu.TcpClient)** - TCP 通信栈（长度头、粘包处理）
- **[Asprtu.Rtu.BACnet](src/Asprtu.Rtu.BACnet)** - BACnet 协议库（MIT）
- **[Asprtu.Rtu](src/Infrastructures/Asprtu.Rtu)** - 基础设施（DI、通道抽象、缓冲队列）

## 快速开始

```bash
# 安装 NuGet 包
dotnet add package Asprtu.Rtu.DLT645
```

## 示例与文档

- [DLT645 示例](sample/Dlt645) - 串口读表、自动协商
- [TCP 示例](sample/Tcp) - 服务端/客户端通信
- [完整文档](src/Asprtu.Rtu.DLT645/README.md)

## 许可证

MIT License - 包含第三方 [BACnet 库](https://github.com/ela-compil/BACnet)（MIT）
