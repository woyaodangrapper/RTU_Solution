using System.Collections.Concurrent;

namespace Aspdcs.Rtu.Contracts.Queue;

public class QueueContext<T>
{
    public ConcurrentQueue<object> Queue { get; } = new();
    public SemaphoreSlim Signal { get; } = new(0);
    public ISubject<T>? Subject { get; set; } = new Subject<T>();
}
