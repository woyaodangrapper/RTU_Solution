using Microsoft.Extensions.Caching.Memory;
using RTU.Infrastructures.Contracts.Queue;
using RTU.Infrastructures.Extensions;
using System.Collections.Concurrent;
using ZiggyCreatures.Caching.Fusion;

namespace RTU.Infrastructures.Queue;

/// <summary>
/// 队列实现，支持 Dispose，极限/常规模式切换
/// </summary>
public abstract class QueueCache<T> : L1Cache
{
    private readonly ConcurrentQueue<object> _queue;
    private readonly SemaphoreSlim _signal;
    private readonly bool _extremeMode;
    private bool _disposed;

    protected QueueCache(QueueOptions queueOptions)
       : base(queueOptions.Name, new FusionCacheOptions()
       {
           DefaultEntryOptions = new FusionCacheEntryOptions
           {
               Duration = queueOptions.Duration,
               IsFailSafeEnabled = queueOptions.IsFailSafeEnabled,
               FailSafeThrottleDuration = queueOptions.FailSafeThrottleDuration,
           },

       }, new MemoryCacheOptions
       {
           SizeLimit = queueOptions.SizeLimit,
           ExpirationScanFrequency = queueOptions.ExpirationScanFrequency,
           CompactionPercentage = queueOptions.CompactionPercentage,
       })
    {
        _extremeMode = queueOptions.Mode;
        _queue = queueOptions.Queue;
        _signal = queueOptions.Signal;
    }
    protected QueueCache(string name, ConcurrentQueue<object> queue, SemaphoreSlim signal)
        : base(
            name,
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
                SizeLimit = 1000,                     // 大约10MB缓存空间
                ExpirationScanFrequency = TimeSpan.FromMinutes(1), // 每1分钟清理过期缓存
                CompactionPercentage = 0.2,                       // 内存压力时，压缩20%
            }
        )
    {
        _queue = queue;
        _signal = signal;
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
            var key = SnowflakeId.NewSnowflakeId(); // 生成唯一 key
            GetOrAdd(key, item, Util.TryOccupy(item), TimeSpan.FromSeconds(30)); // 存入缓存，30秒有效
            _queue.Enqueue(key); // 队列里放 key
        }
    }

    /// <summary>
    /// 弹出单条（非阻塞）
    /// </summary>
    public bool Dequeue(out T result, CancellationToken token)
    {
        result = default!;  // 默认值

        // 检查取消请求
        if (token.IsCancellationRequested)
        {
            // 取消请求时直接返回 false
            return false;
        }

        if (_queue.TryDequeue(out var raw))
        {
            if (_extremeMode)
            {
                // 极限模式：raw 就是 T
                result = (T)raw!;
            }
            else
            {
                var item = Get<T>(raw);
                Remove(raw);

                if (item == null)
                {
                    throw new InvalidOperationException($"缓存未命中或已过期，Key={raw}");
                }

                result = item;
            }

            return true;  // 成功弹出数据
        }
        else
        {
            // 如果队列为空，返回 false
            return false;
        }

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
