using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RTU.Infrastructures.Contracts.Tcp;

namespace RTU.Infrastructures.Queue;

internal class QueueFactory<T> : IQueueFactory<T>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly QueueOptions _queueOptions;

    /// <inheritdoc/>
    public QueueFactory()
        : this(NullLoggerFactory.Instance, QueueOptions.Instance)
    {
    }

    public QueueFactory(ILoggerFactory loggerFactory, QueueOptions queueOptions)
    {
        _loggerFactory = loggerFactory;
        _queueOptions = queueOptions;

    }
    /// <inheritdoc/>
    public IPublisher<T> CreatePublisher(QueueOptions options) =>
        new Publisher<T>(options, _loggerFactory);

    /// <inheritdoc/>
    public ISubscriber<T> CreateSubscriber(QueueOptions options) =>
        new Subscriber<T>(options, _loggerFactory);

    /// <inheritdoc/>
    public IPublisher<T> CreatePublisher() =>
    new Publisher<T>(_queueOptions, _loggerFactory);

    /// <inheritdoc/>
    public ISubscriber<T> CreateSubscriber() =>
        new Subscriber<T>(_queueOptions, _loggerFactory);
}
