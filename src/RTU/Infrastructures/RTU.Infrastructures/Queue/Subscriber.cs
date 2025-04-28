using Microsoft.Extensions.Logging;
using RTU.Infrastructures.Contracts.Tcp;

namespace RTU.Infrastructures.Queue;


public class Subscriber<T> : QueueCache<T>, ISubscriber<T>
{
    private readonly SemaphoreSlim _signal;

    public Subscriber(QueueOptions options, ILoggerFactory _loggerFactory)
      : base(options)
    {
        _signal = options.Signal;
    }
    public Task<T[]> DequeueBatchAsync(CancellationToken cancellation)
    {
        throw new NotImplementedException();
    }

    public bool TryDequeue(out T? message, CancellationToken cancellation)
    {
        message = default;
        try
        {
            message = DequeueCore(cancellation);
            return true;
        }
        catch (OperationCanceledException) // Catch specific exception
        {
            return false;
        }
        finally
        {
            _signal.Release();
        }
    }

    private T DequeueCore(CancellationToken cancellation)
    {

        try
        {
            int i = -5;
            while (true)
            {
                if (Dequeue(out var message, cancellation))
                    return message;

                if (i > 10)
                    _signal.Wait(millisecondsTimeout: 10, cancellation);
                else if (i++ > 0)
                    _signal.Wait(millisecondsTimeout: i, cancellation);
                else
                    Thread.Yield();
            }
        }
        finally
        {
            _signal.Release();
        }
    }

}
