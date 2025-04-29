using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace RTU.Infrastructures.Queue;

internal class QueueFactory<T> : IQueueFactory<T>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly QueueOptions _queueOptions;
    private readonly Subject<T>? _subject;

    /// <inheritdoc/>
    public QueueFactory()
        : this(NullLoggerFactory.Instance, QueueOptions.Instance, new Subject<T>())
    {
    }

    public QueueFactory(ILoggerFactory loggerFactory, QueueOptions queueOptions, Subject<T> subject)
    {
        _loggerFactory = loggerFactory;
        _queueOptions = queueOptions;
        _subject = subject;
    }
    /// <inheritdoc/>
    public IPublisher<T> CreatePublisher(QueueOptions options) =>
        new Publisher<T>(options, _loggerFactory);

    /// <inheritdoc/>
    public ISubscriber<T> CreateSubscriber(QueueOptions options) =>
        new Subscriber<T>(options, _loggerFactory);

    /// <inheritdoc/>
    public IPublisher<T> CreatePublisher() =>
        new Publisher<T>(_queueOptions, _loggerFactory, _subject);

    /// <inheritdoc/>
    public ISubscriber<T> CreateSubscriber() =>
        new Subscriber<T>(_queueOptions, _loggerFactory, _subject);
}
