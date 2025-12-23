using Aspdcs.Rtu.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Aspdcs.Rtu.DLT645;

public sealed class Dlt645ClientFactory(ILoggerFactory? loggerFactory = null) : IDlt645ClientFactory
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

    private static readonly ConcurrentDictionary<string, Dlt645Client> _instance =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Lazy<Dlt645ClientFactory> instance = new(() => new());

    public static Dlt645ClientFactory Instance => instance.Value;

    public IDlt645Client CreateDlt645Client([NotNull] ChannelOptions options)
        => _instance.GetOrAdd(options.ChannelName, key => new Dlt645Client(options, _loggerFactory));

    public ChannelOptions.Builder CreateBuilder([NotNull] string name) => new(name);

    Dlt645Client ILibraryFactory<Dlt645Client>.Create(params object[] args)
    {
        if (args.Length == 0 || args[0] is not ChannelOptions options)
            throw new ArgumentException("需要一个 ChannelOptions 类型参数", nameof(args));

        return (Dlt645Client)CreateDlt645Client(options);
    }

    public bool Remove(string name)
    {
        if (_instance.TryRemove(name, out var client))
        {
            client?.Dispose();
            return true;
        }
        return false;
    }
}