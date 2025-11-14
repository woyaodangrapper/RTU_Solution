using Asprtu.Rtu.DLT645.Contracts;
using Microsoft.Extensions.Logging;

namespace Asprtu.Rtu.DLT645;

public sealed class Dlt645ClientFactory(ILoggerFactory? loggerFactory = null) : IDlt645ClientFactory
{
    public Dlt645Client Create(params object[] args)
    {
        throw new NotImplementedException();
    }

    public bool Remove(string name)
    {
        throw new NotImplementedException();
    }
}