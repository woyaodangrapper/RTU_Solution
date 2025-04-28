using Microsoft.Extensions.DependencyInjection;
using RTU.Infrastructures.Contracts.Queue;
using RTU.Infrastructures.Queue;

namespace RTU.Infrastructures.Extensions.Tcp;

/// <summary>
/// Extensions to the <see cref="IServiceCollection"/> to register the shared-memory queue.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers what is needed to create and consume shared-memory queues that are
    /// cross-process accessible.
    /// Use <see cref="IQueueFactory"/> to access the queue.
    /// </summary>
    public static IServiceCollection AddQueueCache(this IServiceCollection services)
    {
        services.AddTransient(typeof(IQueueFactory<>), typeof(QueueFactory<>));
        return services;
    }
}