using Asprtu.Rtu.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Asprtu.Rtu;

public class LibraryCapacities<T> : ILibraryCapacities<T>
    where T : class, IContracts
{
    public string Name { get; } = typeof(T).Name;
    IContracts ILibraryCapacities.Contracts => Contracts;

    public T Contracts { get; }

    public LibraryCapacities(T instance)
        => Contracts = instance ?? throw new ArgumentNullException(nameof(instance));

    public LibraryCapacities(IServiceProvider provider) =>
        Contracts = ActivatorUtilities.CreateInstance<T>(provider);
}