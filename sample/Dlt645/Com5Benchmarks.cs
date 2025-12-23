using Aspdcs.Rtu.DLT645;
using Aspdcs.Rtu.DLT645.Contracts;
using BenchmarkDotNet.Attributes;

namespace Dlt645.Sample;

/// <summary>
/// 性能基准测试
/// 运行方式: dotnet run -c Release
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public class Benchmarks
{
    private IDlt645Client? _channel;

    [GlobalSetup]
    public void Setup()
    {
        _channel = ChannelOptions.CreateBuilder("BenchmarkChannel")
            .WithChannel("COM5")
            .Run();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _channel?.Dispose();
    }

    /// <summary>
    /// 基准测试: 广播发现设备地址
    /// 测试目标: 从 COM5 广播到所有设备并获得反馈
    /// </summary>
    [Benchmark(Description = "广播发现设备")]
    public async Task<int> BroadcastDiscoverDevices()
    {
        if (_channel == null) return 0;

        int deviceCount = 0;
        await foreach (var address in _channel.TryReadAddressAsync())
        {
            deviceCount++;
            if (deviceCount >= 10) break; // 限制最多10个设备
        }

        return deviceCount;
    }

    /// <summary>
    /// 基准测试: 读取单个设备电量数据
    /// </summary>
    [Benchmark(Description = "读取设备电量")]
    public async Task<int> ReadSingleDeviceEnergy()
    {
        if (_channel == null) return 0;

        int frameCount = 0;
        await foreach (var frame in _channel.ReadAsync("11-11-00-00-00-00"))
        {
            frameCount++;
            if (frameCount >= 5) break; // 读取5个帧
        }

        return frameCount;
    }

    /// <summary>
    /// 基准测试: 完整工作流 - 发现设备并读取数据
    /// </summary>
    [Benchmark(Description = "完整读取流程")]
    public async Task<(int Devices, int Frames)> BroadcastAndReadResponses()
    {
        if (_channel == null) return (0, 0);

        int deviceCount = 0;
        int totalFrames = 0;

        // 发现设备
        await foreach (var address in _channel.TryReadAddressAsync())
        {
            deviceCount++;
            if (deviceCount >= 3) break; // 最多3个设备
        }

        // 读取每个设备的数据
        for (int i = 0; i < deviceCount; i++)
        {
            await foreach (var frame in _channel.ReadAsync("11-11-00-00-00-00"))
            {
                totalFrames++;
                if (totalFrames >= 10) break;
            }
        }

        return (deviceCount, totalFrames);
    }
}
