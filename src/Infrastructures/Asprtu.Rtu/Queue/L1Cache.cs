using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using ZiggyCreatures.Caching.Fusion;

namespace Asprtu.Rtu.Queue;

/// <summary>
/// L1 缓存实现，融合 FusionCache | MemoryCache
/// </summary>
public abstract class L1Cache : IDisposable
{
    private readonly string _name;

    private readonly FusionCache _fusionCache;
    private readonly MemoryCache _memoryCache;
    private readonly FusionCacheEntryOptions _entryOptions;

    protected static readonly ConcurrentDictionary<string, (FusionCache, MemoryCache, FusionCacheEntryOptions)>
        CachePool = [];

    protected L1Cache(QueueOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _name = options.Name;
        if (CachePool.TryGetValue(_name, out var cache))
        {
            _fusionCache = cache.Item1;
            _memoryCache = cache.Item2;
            _entryOptions = cache.Item3;
        }
        else
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = options.SizeLimit,
                ExpirationScanFrequency = options.ExpirationScanFrequency,
                CompactionPercentage = options.CompactionPercentage,
            });
            _fusionCache = new FusionCache(new FusionCacheOptions
            {
                DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Duration = options.Duration,
                    IsFailSafeEnabled = options.IsFailSafeEnabled,
                    FailSafeThrottleDuration = options.FailSafeThrottleDuration,
                }
            }, _memoryCache);
            _entryOptions = new FusionCacheEntryOptions()
            {
                Duration = TimeSpan.FromSeconds(30),
                Size = 1,
                IsFailSafeEnabled = true
            };

            CachePool[_name] = (
               _fusionCache, _memoryCache, _entryOptions
            );
        }
    }

    /// <summary>
    /// 获取缓存项，如果缓存不存在则返回默认值
    /// </summary>
    public TItem? Get<TItem>(object key)
    {
        return _fusionCache.GetOrDefault<TItem>($"RTU:{key}");
    }

    /// <summary>
    /// 设置缓存项
    /// </summary>
    public void Set<TItem>(object key, TItem item, int? size = null, TimeSpan? duration = null)
    {
        var options = _entryOptions;

        if (duration.HasValue)
        {
            options.SetDuration(duration.Value); // 设置默认有效期
        }

        if (size.HasValue)
        {
            options.SetSize(size.Value);
        }
        _fusionCache.Set($"RTU:{key}", item, options);
    }

    public TItem GetOrAdd<TItem>(object key, TItem factory, int? size = null, TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var options = _entryOptions;
        if (duration.HasValue)
        {
            options.SetDuration(duration.Value); // 设置默认有效期
        }

        if (size.HasValue)
        {
            options.SetSize(size.Value);
        }
        // 常规模式，通过 FusionCache 尝试获取
        return _fusionCache.GetOrSet(
            $"RTU:{key}", factory, options
        );
    }

    public void Remove(object key)
    {
        _fusionCache.Remove($"RTU:{key}");
    }

    private bool _disposed;

    // Existing fields and methods...

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                _fusionCache.Dispose();
                _memoryCache.Dispose();
            }

            // Dispose unmanaged resources if any

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~L1Cache()
    {
        Dispose(false);
    }
}