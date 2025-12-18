using Aspdcs.Rtu.Contracts;
using Aspdcs.Rtu.TcpServer.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Aspdcs.Rtu.TcpServer;

public sealed class TcpServerFactory(ILoggerFactory? loggerFactory = null) : ITcpServerFactory
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

    private static readonly ConcurrentDictionary<string, TcpServer> _instance =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Lazy<TcpServerFactory> instance = new(() => new());

    public static TcpServerFactory Instance => instance.Value;

    public ITcpServer CreateTcpServer([NotNull] ChannelOptions options)
        => _instance.GetOrAdd(options.ChannelName, key => new TcpServer(options, _loggerFactory));

    public CreateBuilder CreateBuilder([NotNull] string name) => new(name);

    TcpServer ILibraryFactory<TcpServer>.Create(params object[] args)
    {
        if (args.Length == 0 || args[0] is not ChannelOptions options)
            throw new ArgumentException("需要一个 ChannelOptions 类型参数", nameof(args));

        return (TcpServer)CreateTcpServer(options);
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