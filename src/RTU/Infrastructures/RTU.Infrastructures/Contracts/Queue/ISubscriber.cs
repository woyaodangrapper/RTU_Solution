namespace RTU.Infrastructures.Contracts.Queue;

public interface ISubscriber<T> : IDisposable
{

    bool TryDequeue(out T? message, CancellationToken cancellation);
}