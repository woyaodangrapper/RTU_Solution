namespace Asprtu.Rtu.Queue;

public class Subscriber<T> : QueueCache<T>, ISubscriber<T>
{
    public Subscriber(QueueOptions options, QueueContext<T> context, ILoggerFactory loggerFactory)
      : base(options, context, loggerFactory)
    { }

    public IObservable<T>? Observable => Subject;

    public bool TryDequeue(out T? message)
    {
        message = default;
        Signal.Wait(0);
        try
        {
            return Dequeue(out message, default);
        }
        catch (OperationCanceledException) // Catch specific exception
        {
            return false;
        }
    }

    public bool TryDequeue(out T? message, CancellationToken cancellationToken)
    {
        message = default;

        Signal.Wait(cancellationToken);
        try
        {
            return Dequeue(out message, cancellationToken);
        }
        catch (OperationCanceledException) // Catch specific exception
        {
            return false;
        }
    }

    public async Task<(bool success, T? item)> TryDequeueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (Dequeue(out var item, cancellationToken))
                return (true, item);
            return default;
        }
        catch (OperationCanceledException)
        {
            return default;
        }
    }

    public async Task<IEnumerable<T>> TryDequeueBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        var list = new List<T>(batchSize);
        for (int i = 0; i < batchSize; i++)
        {
            var (success, item) = await TryDequeueAsync(cancellationToken).ConfigureAwait(false);
            if (success && item is not null) list.Add(item);
            else break;
        }
        return list;
    }

    public bool TryDequeueBatch(out IEnumerable<T> values, int batchSize, CancellationToken cancellationToken)
    {
        var list = new List<T>();

        for (int i = 0; i < batchSize; i++)
        {
            if (TryDequeue(out var item, cancellationToken) && item is not null)
            {
                list.Add(item);
            }
            else
            {
                // 极端竞态：信号到达但未取到元素，则跳出
                break;
            }
        }

        values = list;
        return list.Count > 0;
    }

    public IList<T> DequeueAll()
    {
        var list = new List<T>();
        while (TryDequeue(out var item) && item is not null)
        {
            list.Add(item);
            Signal.Wait();
        }
        return list;
    }
}