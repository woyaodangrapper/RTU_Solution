using Microsoft.Extensions.Logging;
using RTU.Infrastructures.Contracts.Queue;
using System.Reactive.Subjects;

namespace RTU.Infrastructures.Queue;


public class Subscriber<T> : QueueCache<T>, ISubscriber<T>
{
    private readonly SemaphoreSlim _signal;

    public Subscriber(QueueOptions options, ILoggerFactory loggerFactory, Subject<T>? subject = null)
      : base(options, loggerFactory, subject)
    {
        _signal = options.Signal;
    }

    public IObservable<T>? Observable => Subject;

    public bool TryDequeue(out T? message)
    {
        message = default;
        _signal.Wait(0);
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

        _signal.Wait(cancellationToken);
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
            await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (Dequeue(out var item, cancellationToken))
                return (true, item);
            return default;
        }
        catch (OperationCanceledException)
        {
            return default;
        }
    }

    public async Task<List<T>> TryDequeueBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        var list = new List<T>(batchSize);
        for (int i = 0; i < batchSize; i++)
        {
            var (success, item) = await TryDequeueAsync(cancellationToken).ConfigureAwait(false);
            if (success) list.Add(item);
            else break;
        }
        return list;
    }

    public List<T> TryDequeueBatch(int batchSize, CancellationToken cancellationToken)
    {
        var list = new List<T>(batchSize);
        for (int i = 0; i < batchSize; i++)
        {
            if (TryDequeue(out var item, cancellationToken))
            {
                list.Add(item);
            }
            else
            {
                // 极端竞态：信号到达但未取到元素，则跳出
                break;
            }
        }
        return list;
    }

    public IList<T> DequeueAll()
    {
        var list = new List<T>();
        while (TryDequeue(out var item))
        {
            list.Add(item);
            _signal.Wait();
        }
        return list;
    }

}
