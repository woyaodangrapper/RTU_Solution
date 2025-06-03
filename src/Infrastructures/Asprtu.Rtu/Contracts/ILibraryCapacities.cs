namespace Asprtu.Rtu.Contracts;

public interface ILibraryCapacities
{
    string Name { get; }
    IContracts Contracts { get; }
}

public interface ILibraryCapacities<T> : ILibraryCapacities
    where T : IContracts
{
    new T Contracts { get; }
}