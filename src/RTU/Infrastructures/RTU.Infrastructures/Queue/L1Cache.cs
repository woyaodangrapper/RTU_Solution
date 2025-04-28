using Microsoft.Extensions.Caching.Memory;
using ZiggyCreatures.Caching.Fusion;

namespace RTU.Infrastructures.Queue;

/// <summary>
/// L1 缓存实现，融合 FusionCache & MemoryCache
/// </summary>
public class L1Cache : IDisposable
{
    private readonly FusionCache _fusionCache;
    private readonly MemoryCache _memoryCache;
    private readonly FusionCacheEntryOptions _entryOptions;

    public L1Cache(FusionCacheOptions fusionOptions, MemoryCacheOptions memoryOptions)
    {
        _memoryCache = new MemoryCache(memoryOptions);
        _fusionCache = new FusionCache(fusionOptions, _memoryCache);
        _entryOptions = new FusionCacheEntryOptions()
        {
            Duration = TimeSpan.FromSeconds(30),
            IsFailSafeEnabled = true
        };
    }

    /// <summary>
    /// 获取缓存项，如果缓存不存在则返回默认值
    /// </summary>
    public TItem? Get<TItem>(string key)
    {
        return _fusionCache.GetOrDefault<TItem>(key);
    }

    /// <summary>
    /// 设置缓存项
    /// </summary>
    public void Set<TItem>(string key, TItem item, TimeSpan? duration = null)
    {
        var options = _entryOptions;
        if (duration.HasValue)
        {
            options.SetDuration(duration.Value); // 设置默认有效期
        }
        _fusionCache.Set(key, item, options);
    }


    public TItem GetOrAdd<TItem>(object key, TItem factory, TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var options = _entryOptions;
        if (duration.HasValue)
        {
            options.SetDuration(duration.Value); // 设置默认有效期
        }
        // 常规模式，通过 FusionCache 尝试获取
        return _fusionCache.GetOrSet<TItem>(
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

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~L1Cache()
    {
        Dispose(false);
    }
}
