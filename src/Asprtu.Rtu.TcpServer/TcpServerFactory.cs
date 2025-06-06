using Asprtu.Rtu.TcpServer.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Asprtu.Rtu.TcpServer;

public class TcpServerFactory : ITcpServerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ChannelOptions _channelOptions;

    private static readonly ConcurrentDictionary<string, ChannelOptions> _instance
        = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Lazy<TcpServerFactory> instance = new(() => new());

    public static TcpServerFactory Instance => instance.Value;

    public TcpServerFactory(string name = "default")
       : this(NullLoggerFactory.Instance, new ChannelOptions(name))
    {
    }

    public TcpServerFactory(ILoggerFactory loggerFactory, ChannelOptions options)
    {
        _loggerFactory = loggerFactory;
        _channelOptions = options;
    }

    private static ChannelOptions GetOrCreate(string name)
        => _instance.GetOrAdd(name, _ => new ChannelOptions(name));

    private static ChannelOptions AddOrCreate(string name, ChannelOptions channel)
        => _instance.GetOrAdd(name, _ => channel);

    public ITcpServer CreateTcpServer()
        => new TcpServer(_channelOptions, _loggerFactory);

    public ITcpServer CreateTcpServer([NotNull] ChannelOptions options)
        => new TcpServer(AddOrCreate(options.ChannelName, options), _loggerFactory);

    public TcpServer Create(string name) => new(GetOrCreate(name), _loggerFactory);

    public bool Remove(string name) => _instance.Remove(name, out var _);
}