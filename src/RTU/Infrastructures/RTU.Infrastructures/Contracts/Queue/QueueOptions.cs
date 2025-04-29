using System.Collections.Concurrent;

namespace RTU.Infrastructures.Contracts.Queue;

/// <summary> The options to create a queue. </summary>
/// <summary>
/// 队列配置选项
/// </summary>
public sealed class QueueOptions
{
    public string Name { get; set; } = "RTU.Queue";

    /// <summary>
    /// 缓存大小队列数量最多 1000 条数据
    /// </summary>
    public long SizeLimit { get; set; } = 1000;

    /// <summary>
    /// 扫描过期缓存的频率。默认每1分钟扫描一次。
    /// </summary>
    public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// 压缩百分比，当达到大小限制时，移除最旧的缓存数据比例。默认20%。
    /// </summary>
    public double CompactionPercentage { get; set; } = 0.2;

    /// <summary>
    /// 是否启用极限模式。默认关闭（false）。
    /// 极限模式下，缓存对象直接存储为T类型，无需额外查找。
    /// </summary>
    public bool Mode { get; set; }

    /// <summary>
    /// 单条缓存默认存活时间。默认30秒。
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 是否启用 Fail-Safe 容错机制。默认启用（true）。
    /// 当出现异常时，可以短时间返回旧值而不是抛出错误。
    /// </summary>
    public bool IsFailSafeEnabled { get; set; } = true;

    /// <summary>
    /// Fail-Safe 模式下的限流时间间隔。默认5秒。
    /// 在此时间内不会重复触发恢复操作。
    /// </summary>
    public TimeSpan FailSafeThrottleDuration { get; set; } = TimeSpan.FromSeconds(5);

    public ConcurrentQueue<object> Queue { get; } = new();

    public SemaphoreSlim Signal { get; } = new(0);

    private static QueueOptions? _instance;

    public static QueueOptions Instance => _instance ??= new QueueOptions();
}