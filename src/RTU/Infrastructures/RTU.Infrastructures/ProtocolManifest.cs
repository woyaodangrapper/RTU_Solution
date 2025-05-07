using RTU.Infrastructures.Contracts;

namespace RTU.Infrastructures;

public class ProtocolManifest<T> : IProtocolManifest
    where T : IProtocol, new()
{
    public string Name { get; } = typeof(T).Name;
    public IProtocol Protocol { get; } = new T();
}