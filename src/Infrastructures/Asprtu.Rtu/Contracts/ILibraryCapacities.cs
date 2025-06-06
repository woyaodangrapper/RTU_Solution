namespace Asprtu.Rtu.Contracts;

public interface ILibraryCapacities
{
    string Name { get; }
    IContracts Contracts { get; }
}

public interface ILibraryCapacities<out T> : ILibraryCapacities
    where T : IContracts
{
    new T Contracts { get; }
}