namespace RTU.Infrastructures.Contracts;
public interface IProtocolManifest
{
    string Name { get; }
    IProtocol Protocol { get; }
}
