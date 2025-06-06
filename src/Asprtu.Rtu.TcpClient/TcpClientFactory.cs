using Asprtu.Rtu.TcpClient.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Asprtu.Rtu.TcpClient;

public class TcpClientFactory : ITcpClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ChannelOptions _channelOptions;

    private static readonly ConcurrentDictionary<string, ChannelOptions> _instance
        = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Lazy<TcpClientFactory> instance = new(() => new());

    public static TcpClientFactory Instance => instance.Value;

    public TcpClientFactory(string name = "default")
        : this(NullLoggerFactory.Instance, new ChannelOptions(name))
    {
    }

    public TcpClientFactory(ILoggerFactory loggerFactory, ChannelOptions options)
    {
        _loggerFactory = loggerFactory;
        _channelOptions = options;
    }

    private static ChannelOptions GetOrCreate(string name)
        => _instance.GetOrAdd(name, _ => new ChannelOptions(name));

    private static ChannelOptions AddOrCreate(string name, ChannelOptions channel)
        => _instance.GetOrAdd(name, _ => channel);

    public ITcpClient CreateTcpClient()
        => new TcpClient(_channelOptions, _loggerFactory);

    public ITcpClient CreateTcpClient([NotNull] ChannelOptions options)
        => new TcpClient(AddOrCreate(options.ChannelName, options), _loggerFactory);

    public TcpClient Create(string name) => new(GetOrCreate(name), _loggerFactory);

    public bool Remove(string name) => _instance.Remove(name, out var _);
}