using Microsoft.Extensions.Logging;
using RTU.Infrastructures.Contracts.Queue;

namespace RTU.Infrastructures.Queue;

public class Publisher<T> : QueueCache<T>, IPublisher<T>
{
    private readonly SemaphoreSlim _signal;

    public Publisher(QueueOptions options, ILoggerFactory _loggerFactory)
      : base(options)
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
