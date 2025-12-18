using Aspdcs.Rtu.Contracts;
using Aspdcs.Rtu.TcpClient.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Aspdcs.Rtu.TcpClient;

public sealed class TcpClientFactory(ILoggerFactory? loggerFactory = null) : ITcpClientFactory
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

    private static readonly ConcurrentDictionary<string, TcpClient> _instance =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Lazy<TcpClientFactory> instance = new(() => new());

    public static TcpClientFactory Instance => instance.Value;

    public ITcpClient CreateTcpClient([NotNull] ChannelOptions options)
        => _instance.GetOrAdd(options.ChannelName, key => new TcpClient(options, _loggerFactory));

    public CreateBuilder CreateBuilder([NotNull] string name) => new(name);

    TcpClient ILibraryFactory<TcpClient>.Create(params object[] args)
    {
        if (args.Length == 0 || args[0] is not ChannelOptions options)
            throw new ArgumentException("需要一个 ChannelOptions 类型参数", nameof(args));

        return (TcpClient)CreateTcpClient(options);
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