# Asprtu RTU Solution

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)

A multi-protocol RTU communication toolkit for .NET 9, supporting DLT645, TCP, BACnet and more industrial protocols.

English | [简体中文](README.md)

## Core Components

- **[Asprtu.Rtu.DLT645](src/Asprtu.Rtu.DLT645)** - DLT645 power meter communication (auto serial negotiation, async frame parsing)
- **[Asprtu.Rtu.TcpServer](src/Asprtu.Rtu.TcpServer)** / **[TcpClient](src/Asprtu.Rtu.TcpClient)** - TCP stack (length header, sticky packet handling)
- **[Asprtu.Rtu.BACnet](src/Asprtu.Rtu.BACnet)** - BACnet protocol library (MIT)
- **[Asprtu.Rtu](src/Infrastructures/Asprtu.Rtu)** - Infrastructure (DI, channel abstraction, buffer queues)

## Quick Start

```bash
# Install NuGet package
dotnet add package Asprtu.Rtu.DLT645
```

## Examples & Documentation

- [DLT645 Examples](sample/Dlt645) - Serial meter reading, auto negotiation
- [TCP Examples](sample/Tcp) - Server/client communication
- [Full Documentation](src/Asprtu.Rtu.DLT645/README.en.md)

## License

MIT License - includes third-party [BACnet Library](https://github.com/ela-compil/BACnet) (MIT)
