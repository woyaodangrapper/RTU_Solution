# Asprtu.Rtu

Infrastructure library for .NET 9 RTU communication, providing unified abstractions and tooling for protocols like DLT645, TCP, and BACnet.

## Features

- **Channel Abstraction** - Unified communication channel interface supporting serial ports, TCP, UDP, and more
- **Message Queue** - Asynchronous message processing with backpressure control and priority scheduling
- **Dependency Injection** - Complete DI extensions for easy integration with ASP.NET Core or hosted services
- **Caching Support** - High-performance distributed caching abstraction based on FusionCache
- **Reactive Extensions** - Built-in System.Reactive support for reactive programming patterns

## Quick Start

```csharp
using Asprtu.Rtu.Contracts;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddRtuServices(); // Register RTU infrastructure services

var provider = services.BuildServiceProvider();
```

## Components

- `Contracts/` - Core interfaces and contract definitions
- `Attributes/` - Attribute markers (e.g., LibraryCapacities)
- `Extensions/` - Extension method collections
- `Queue/` - Message queue implementations

## Dependencies

- Microsoft.Extensions.Hosting.Abstractions
- Microsoft.Extensions.Caching.Memory
- ZiggyCreatures.FusionCache
- System.Reactive

## License

MIT License - Copyright © 2025 Asprtu
