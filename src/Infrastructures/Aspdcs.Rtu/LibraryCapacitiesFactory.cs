using Aspdcs.Rtu.Contracts;
using System.Collections.Concurrent;

namespace Aspdcs.Rtu;

public class LibraryCapacitiesFactory<T>(ILibraryFactory<T> factory) : ILibraryCapacitiesFactory<T>
    where T : class, IContracts
{
    private readonly ILibraryFactory<T> _factory = factory;

    private readonly ConcurrentDictionary<string, T> _dict = new(StringComparer.OrdinalIgnoreCase);

    public T Add(string name, params object[] args)
    {
        var instance = _factory.Create(args)
            ?? throw new InvalidOperationException($"工厂未能创建出名称为 '{args}' 的 {typeof(T).Name} 实例");

        if (!_dict.TryAdd(name, instance))
        {
            (instance as IDisposable)?.Dispose();
            throw new InvalidOperationException($"已存在同名的 {typeof(T).Name}：{args}");
        }

        return instance;
    }

    public bool Remove(string name)
    {
        _factory.Remove(name);

        if (_dict.TryRemove(name, out var instance))
        {
            (instance as IDisposable)?.Dispose();
            return true;
        }

        return false;
    }

    public IReadOnlyCollection<T> All => [.. _dict.Values];
}