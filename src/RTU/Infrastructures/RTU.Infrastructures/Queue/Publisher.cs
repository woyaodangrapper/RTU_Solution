using Microsoft.Extensions.Logging;

namespace RTU.Infrastructures.Queue;

public class Publisher<T> : QueueCache<T>, IPublisher<T>
{
    private readonly SemaphoreSlim _signal;

    public Publisher(QueueOptions options, ILoggerFactory loggerFactory, Subject<T>? subject = null)
      : base(options, loggerFactory, subject)
    {
        _signal = options.Signal;
    }

    /// <summary>
    /// 向队列中添加消息
    /// </summary>
    /// <param name="message">待发布的消息</param>
    /// <returns>如果成功加入队列则返回true，否则返回false</returns>
    public bool TryEnqueue(T message)
    {
        Subject?.OnNext(message);

        try
        {
            Enqueue(message);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        finally
        {
            _signal.Release();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}