namespace Asprtu.Rtu.Contracts;

public interface ILibraryFactory<T> where T : class, IContracts
{
    T Create(params object[] args);

    bool Remove(string name);
}