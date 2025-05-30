using Asprtu.Rtu.Contracts;

namespace Asprtu.Rtu;

public class LibraryCapacities<T> : ILibraryCapacities
    where T : IContracts, new()
{
    public string Name { get; } = typeof(T).Name;
    public IContracts Contracts { get; } = new T();
}