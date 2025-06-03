namespace Asprtu.Rtu.Contracts;

public interface ILibraryFactory<T> where T : class, IContracts
{
    T Create(string name);

    bool Remove(string name);
}