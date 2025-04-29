using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
    protected ILogger<QueueCache<T>> Logger { get; }
    protected Subject<T>? Subject { get; }

    protected QueueCache(QueueOptions options, ILoggerFactory loggerFactory, Subject<T>? subject = null)
#pragma warning disable CA1062 // 子类验证过了
      : base(options.Name, new FusionCacheOptions
#pragma warning restore CA1062 // 验证公共方法的参数
      {
          DefaultEntryOptions = new FusionCacheEntryOptions
          {
              Duration = options.Duration,
              IsFailSafeEnabled = options.IsFailSafeEnabled,
              FailSafeThrottleDuration = options.FailSafeThrottleDuration,
          }
      }, new MemoryCacheOptions
      {
          SizeLimit = options.SizeLimit,
          ExpirationScanFrequency = options.ExpirationScanFrequency,
          CompactionPercentage = options.CompactionPercentage,
      })
    {
        ArgumentNullException.ThrowIfNull(options);
        _extremeMode = options.Mode;
        _queue = options.Queue;
        _signal = options.Signal;

        Subject = subject;
        Logger = loggerFactory.CreateLogger<QueueCache<T>>();
    }

    /// <summary>
    /// 入队（非阻塞）
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
                Subject?.Dispose();
            }

            // 释放非托管资源（如果有）

            _disposed = true;
            base.Dispose(disposing);
        }
    }
}