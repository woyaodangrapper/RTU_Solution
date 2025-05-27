using Asprtu.Rtu.Contracts;

namespace Asprtu.Rtu;

public class ProtocolManifest<T> : IProtocolManifest
    where T : IProtocol, new()
{
    public string Name { get; } = typeof(T).Name;
    public IProtocol Protocol { get; } = new T();
}