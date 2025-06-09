namespace Asprtu.Rtu.Contracts;

public interface ILibraryCapacitiesFactory<out T>
    where T : class, IContracts
{
    IReadOnlyCollection<T> All { get; }

    T Add(string name, params object[] args);

    bool Remove(string name);
}