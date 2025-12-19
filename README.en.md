# Aspdcs RTU Solution

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-6%2B-purple.svg)](https://dotnet.microsoft.com/)

Production-ready multi-protocol RTU communication library, supporting .NET 6+ / .NET Framework 4.6.2+

English | [简体中文](README.md)

## ✨ Core Features

- **[Aspdcs.Rtu.DLT645](src/Aspdcs.Rtu.DLT645)** - DLT645-2007 Power Meter Communication
  - ✅ Full protocol implementation (±0x33 encryption, BCD decoding, checksum)
  - ✅ Zero-copy architecture (20-40% performance boost)
  - ✅ Smart response (unicast fast return, broadcast adaptive)
  - 📝 Translated and enhanced from [WKJay/DLT645](https://github.com/WKJay/DLT645)

- **[Aspdcs.Rtu.TcpServer](src/Aspdcs.Rtu.TcpServer)** / **[TcpClient](src/Aspdcs.Rtu.TcpClient)** - TCP Communication Stack
  - ✅ Sticky packet / fragmented packet handling
  - ✅ Length-header protocol
  - ✅ Async high-performance

- **[Aspdcs.Rtu.BACnet](src/Aspdcs.Rtu.BACnet)** - BACnet Protocol Library (MIT License)

- **[Aspdcs.Rtu](src/Infrastructures/Aspdcs.Rtu)** - Infrastructure
  - Dependency injection support
  - Channel abstraction
  - Circular buffer

## Quick Start

### Installation

```bash
dotnet add package Aspdcs.Rtu.DLT645
```

### Sample Code

```csharp
using Aspdcs.Rtu.DLT645;
using Microsoft.Extensions.Logging;

// Create client
var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddConsole());

var options = new ChannelOptions.CreateBuilder("Meter")
    .WithChannel("COM5", 2400, Parity.Even)
    .WithTimeout(TimeSpan.FromSeconds(2))
    .Build();

using var client = new Dlt645Client(options, loggerFactory);

// Read energy data
byte[] address = { 0x11, 0x11, 0x00, 0x00, 0x00, 0x00 };
uint dataId = 0x00010000; // Forward active total energy

await foreach (var value in client.ReadAsync(address, dataId))
{
    Console.WriteLine($"Energy: {value}");
}
```

## 📚 Documentation & Examples

- [DLT645 Full Documentation](src/Aspdcs.Rtu.DLT645/README.md)
- [DLT645 Sample Code](sample/Dlt645)
- [TCP Sample Code](sample/Tcp)
- [Protocol Completion Assessment](docs/LICENSE/DLT645_协议栈完成度.md)

## License

MIT License - includes third-party [BACnet Library](https://github.com/ela-compil/BACnet) (MIT)
