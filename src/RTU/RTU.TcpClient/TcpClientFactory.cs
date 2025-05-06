using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RTU.TcpClient.Contracts;
using System.Collections.Concurrent;

namespace RTU.TcpClient;

public class TcpClientFactory : ITcpClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ChannelOptions _channelOptions;


    private static readonly ConcurrentDictionary<string, ChannelOptions> _instance = new();
    private static readonly Lazy<TcpClientFactory> instance = new(() => new());

    public static TcpClientFactory Instance => instance.Value;

    public TcpClientFactory(string name = "default")
        : this(NullLoggerFactory.Instance, GetOrCreate(name))
    {
    }

    public TcpClientFactory(ILoggerFactory loggerFactory, ChannelOptions options)
    {
        _loggerFactory = loggerFactory;
        _channelOptions = options;
    }

    private static ChannelOptions GetOrCreate(string name)
    {
        return _instance.GetOrAdd(name, _ => new ChannelOptions(name));
    }

    public ITcpClient CreateTcpClient()
     => new TcpClient(_channelOptions, _loggerFactory);

    public ITcpClient CreateTcpClient(ChannelOptions options)
     => new TcpClient(_channelOptions, _loggerFactory);
}
