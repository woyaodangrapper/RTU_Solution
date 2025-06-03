namespace Asprtu.Rtu.Contracts;

public interface ILibraryCapacitiesFactory<T>
    where T : class, IContracts
{
    IReadOnlyCollection<T> All { get; }

    T Add(string name);

    bool Remove(string name);
}