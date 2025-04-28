using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using ZiggyCreatures.Caching.Fusion;

namespace RTU.Infrastructures.Queue;

/// <summary>
/// 队列实现，支持 Dispose，极限/常规模式切换
/// </summary>
public class Queue<T> : L1Cache
{
    private readonly ConcurrentQueue<object> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly bool _extremeMode;
    private bool _disposed;

    public Queue(FusionCacheOptions fusionOptions, MemoryCacheOptions memoryOptions, bool extremeMode)
       : base(fusionOptions, memoryOptions)
    {
        _extremeMode = extremeMode;
    }
    public Queue()
        : base(
            new FusionCacheOptions
            {
                DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromSeconds(30),           // 默认缓存30秒
                    IsFailSafeEnabled = true,                      // 开启Fail-Safe，容错机制
                    FailSafeThrottleDuration = TimeSpan.FromSeconds(5), // 失败时，5秒内不用再触发恢复
                },
            },
            new MemoryCacheOptions
            {
                SizeLimit = 1024 * 1024 * 10,                     // 大约10MB缓存空间
                ExpirationScanFrequency = TimeSpan.FromMinutes(1), // 每1分钟清理过期缓存
                CompactionPercentage = 0.2,                       // 内存压力时，压缩20%
            }
        )
    {
    }

    /// <summary>
    /// 入队
    /// </summary>
    public void Enqueue(T item)
    {
        if (_extremeMode)
        {
            // 极限模式：直接入队 item
            _queue.Enqueue(item!);
        }
        else
        {
            // 常规模式：存缓存，入队 key
            var key = Guid.NewGuid().ToString(); // 生成唯一 key
            GetOrAdd(key, item, TimeSpan.FromSeconds(30)); // 存入缓存，30秒有效
            _queue.Enqueue(key); // 队列里放 key
        }

        _signal.Release(); // 通知有新数据
    }

    /// <summary>
    /// 弹出单条（阻塞）
    /// </summary>
    public async Task<T> DequeueAsync(CancellationToken token)
    {
        await _signal.WaitAsync(token).ConfigureAwait(false);

        if (_queue.TryDequeue(out var raw))
        {
            if (_extremeMode)
            {
                // 极限模式：raw 就是 T
                return (T)raw!;
            }
            else
            {
                // 常规模式：raw 是 key，去缓存取值
                var key = (string)raw!;
                var item = Get<T>(key);
                Remove(key);
                return item == null ? throw new InvalidOperationException($"缓存未命中或已过期，Key={key}") : item;
            }
        }
        else
        {
            throw new InvalidOperationException("信号量触发但队列为空");
        }
    }

    /// <summary>
    /// 批量弹出（阻塞，最多 maxBatchSize 条）
    /// </summary>
    public async Task<T[]> DequeueBatchAsync(int maxBatchSize, CancellationToken token)
    {
        var result = new List<T>();

        for (int i = 0; i < maxBatchSize; i++)
        {
            try
            {
                var item = await DequeueAsync(token).ConfigureAwait(false);
                result.Add(item);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        return [.. result];
    }


    /// <summary>
    /// 释放资源
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 释放托管资源
                _signal.Dispose();
            }

            // 释放非托管资源（如果有）

            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
