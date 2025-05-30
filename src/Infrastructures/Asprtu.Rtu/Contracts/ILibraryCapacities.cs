namespace Asprtu.Rtu.Contracts;

public interface ILibraryCapacities
{
    string Name { get; }
    IContracts Contracts { get; }
}