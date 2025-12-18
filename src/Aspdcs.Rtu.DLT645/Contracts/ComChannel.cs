using System.Collections.ObjectModel;

namespace Aspdcs.Rtu.DLT645.Contracts;

public class ComChannel(string port, byte[]? addresses)
{
    public string Port { get; set; } = port;
    public ReadOnlyCollection<byte> Addresses { get; } = new ReadOnlyCollection<byte>(addresses ?? []);

    public override bool Equals(object? obj)
         => obj is ComChannel other && Port == other.Port;

    public override int GetHashCode()
        => Port.GetHashCode(StringComparison.Ordinal);
}