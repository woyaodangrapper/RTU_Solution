# Aspdcs.Rtu.DLT645

[![NuGet](https://img.shields.io/nuget/v/Aspdcs.Rtu.DLT645.svg)](https://www.nuget.org/packages/Aspdcs.Rtu.DLT645)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](../../LICENSE)

A .NET 9 library for DLT645 power meter communication, featuring automatic serial port negotiation, async frame parsing, and zero-copy optimization.

## Key Features

- **Automatic Serial Port Negotiation**: Auto-detect baud rate and parity combinations
- **Async Frame Assembly**: Complete half-packet/sticky-packet handling with Span/Memory zero-copy
- **Broadcast Address Reading**: Automatically read unknown device addresses
- **Dependency Injection**: Out-of-the-box DI support with singleton factory pattern
- **Observability**: Comprehensive logging for debugging and monitoring

## Quick Start

### Installation

```bash
dotnet add package Aspdcs.Rtu.DLT645
```

### Basic Usage

```csharp
using Aspdcs.Rtu.DLT645;
using Aspdcs.Rtu.DLT645.Contracts;
using Microsoft.Extensions.Logging;

// Create logger factory
var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

// Create channel
var channel = new CreateBuilder("MyChannel")
    .WithChannel("COM5")           // Serial port
    .WithBaudRate(2400)            // Optional: baud rate
    .WithParity(Parity.Even)       // Optional: parity
    .WithLogger(loggerFactory)
    .Run();

// Broadcast read address
await foreach (var frame in await channel.TryReadAddressAsync())
{
    Console.WriteLine($"Device Address: {BitConverter.ToString(frame.Address)}");
}

// Read data item (e.g., voltage)
var result = await channel.TryReadAsync(
    address: new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 },
    dataId: 0x02010100  // DLT645-2007 voltage data identifier
);
```

### Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register DLT645 service
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

## Advanced Features

### Automatic Serial Port Negotiation

When device serial parameters are unknown:

```csharp
var channel = new CreateBuilder("AutoNegotiate")
    .WithChannel("COM5")
    .WithAutoNegotiate()           // Enable auto-negotiation
    .WithLogger(loggerFactory)
    .Run();
```

Supported baud rates: 300, 600, 1200, 2400, 4800, 9600, 19200  
Supported parity: Even, Odd, None

### Zero-Copy Optimization

Use `Span<byte>` and `Memory<byte>` to reduce memory allocation:

```csharp
// Use Span<byte> to avoid array allocation
Span<byte> address = stackalloc byte[6] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
var result = await channel.TryReadAsync(address, 0x02010100);
```

### Batch Reading

```csharp
var dataIds = new uint[] 
{ 
    0x02010100,  // Phase A voltage
    0x02010200,  // Phase B voltage
    0x02010300   // Phase C voltage
};

foreach (var dataId in dataIds)
{
    var result = await channel.TryReadAsync(address, dataId);
    if (result.Success)
    {
        Console.WriteLine($"Data {dataId:X8}: {BitConverter.ToString(result.Data)}");
    }
}
```

## Protocol Support

### DLT645-1997

- ✅ Read data
- ✅ Broadcast time sync
- ✅ Read frozen data
- ⚠️ Write data (partial support)

### DLT645-2007

- ✅ Read data
- ✅ Read subsequent frames
- ✅ Broadcast read address
- ✅ Broadcast time sync
- ⚠️ Write data (partial support)
- ⚠️ Change password (planned)
- ⚠️ Parameter setting (planned)

## Project Status

Current completion: **85%** 🔵🔵🔵🔵🟡

| Module | Completion | Status |
|--------|:----------:|:------:|
| Channel layer | 85% | ✅ |
| Client | 80% | ✅ |
| Factory pattern | 95% | ✅ |
| Frame parsing | 95% | ✅ |
| Port negotiation | 90% | ✅ |
| Read functions | 90% | ✅ |
| Write functions | 75% | ⚠️ |
| Unit tests | 5% | ❌ |

See [Completion Document](../../docs/LICENSE/DLT645_协议栈完成度.md) for details

## Known Limitations

- Write functions need more testing and error handling
- Low unit test coverage
- Multi-device concurrent access needs more validation

## Sample Project

Complete examples at [sample/Dlt645](../../sample/Dlt645)

## Performance

- Zero-copy optimization improvement: 20-40%
- Supports async concurrent operations
- ArrayPool memory reuse, reduces GC pressure

## Dependencies

- .NET 9.0
- RJCP.SerialPortStream 3.0.4
- Microsoft.Extensions.Logging.Abstractions

## Roadmap

- [x] Basic read functionality
- [x] Automatic serial port negotiation
- [x] Zero-copy optimization
- [x] Dependency injection support
- [ ] Improve write functionality
- [ ] Increase unit test coverage
- [ ] Support multi-device concurrency
- [ ] Performance benchmarks

## License

MIT License - see [LICENSE](../../LICENSE) file

## Contributing

Issues and Pull Requests are welcome!

## Support

- GitHub Issues: https://github.com/woyaodangrapper/RTU_Solution/issues
- Documentation: [docs](../../docs)
