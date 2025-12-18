using Aspdcs.Rtu.Extensions;
using System.Collections.Concurrent;

namespace Aspdcs.Rtu.Queue;

/// <summary>
/// 队列实现，支持 Dispose，极限/常规模式切换
/// </summary>
public abstract class QueueCache<T> : L1Cache
{
    private ConcurrentQueue<object> Queue { get; }
    protected SemaphoreSlim Signal { get; }

    private readonly bool _mode;

    private bool _disposed;
    protected ILogger<QueueCache<T>> Logger { get; }
    protected ISubject<T>? Subject { get; }

    protected QueueCache(QueueOptions options, QueueContext<T> context, ILoggerFactory loggerFactory)
      : base(options)
    {
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfNull(context);
        _mode = options.Mode;

        Queue = context.Queue;
        Signal = context.Signal;
        Subject = context.Subject;
        Logger = loggerFactory.CreateLogger<QueueCache<T>>();
    }

    /// <summary>
    /// 入队（非阻塞）
    /// </summary>
    public void Enqueue(T item)
    {
        if (_mode)
        {
            // 极限模式：直接入队 item
            Queue.Enqueue(item!);
        }
        else
        {
            // 常规模式：存缓存，入队 key
            var key = SnowflakeId.NewSnowflakeId(); // 生成唯一 key
            GetOrAdd(key, item, MemorySizeCalculator.TryOccupy(item), TimeSpan.FromSeconds(30)); // 存入缓存，30秒有效
            Queue.Enqueue(key); // 队列里放 key
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

        if (Queue.TryDequeue(out var raw))
        {
            if (_mode)
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
                Signal.Dispose();
                if (Subject is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            // 释放非托管资源（如果有）

            _disposed = true;
            base.Dispose(disposing);
        }
    }
}