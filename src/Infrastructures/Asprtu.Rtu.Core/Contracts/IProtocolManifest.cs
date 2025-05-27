namespace Asprtu.Rtu.Contracts;
public interface IProtocolManifest
{
    string Name { get; }
    IProtocol Protocol { get; }
}
